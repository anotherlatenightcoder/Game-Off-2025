using System.Collections;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class Button_Approve : MonoBehaviour, ISceneInitializable
    {
        [Header("References")]
        [SerializeField] private BigButton _bigButton;
        [SerializeField] private IndicatorLight _indicatorLight;
        
        [Header("Glass Cover (Code Animation)")]
        [SerializeField] private Transform _glassCover;
        [SerializeField] private float _glassAnimationDuration = 0.4f;
        [SerializeField] private float _glassOpenAngle = -90f;

        [Header("Emission Object")]
        [SerializeField] private Renderer _emissionRenderer;
        [SerializeField] private string _emissionProperty = "_EmissionColor";
        [SerializeField] private Color _emissionColor = Color.white;

        [Header("Interaction")]
        [SerializeField] private string interactionText = "Approve [E]";
        [SerializeField] private string _buttonSoundString = "";

        private GameManager _gameManager;
        private EventHub _eventHub;
        private bool _isActive = false;
        private Coroutine _glassRoutine;
        private Quaternion _closedRot;
        private Quaternion _openRot;
        private Material _emissionMaterialInstance;
        private bool _keycodeConfirmed = false;
        private bool _waveConfirmed = false;

        public void SceneInitialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            if (_emissionRenderer)
                _emissionMaterialInstance = _emissionRenderer.material;
            
            _eventHub.Subscribe<InspectionKeypadCodeMatchedEvent>(evt =>
            {
                _keycodeConfirmed = true;
                
                if (_keycodeConfirmed && _waveConfirmed)
                    Activate();
            });
            
            _eventHub.Subscribe<WaveMatchedEvent>(evt =>
            {
                _waveConfirmed = true;
                
                if (_keycodeConfirmed && _waveConfirmed)
                    Activate();
            });
            
            _eventHub.Subscribe<InspectionCompletedEvent>(e => Deactivate());
            _eventHub.Subscribe<ShipArrivedForInspectionEvent>(e => Deactivate());
            _eventHub.Subscribe<DayEndedEvent>(e => Deactivate());

            if (_bigButton)
            {
                _bigButton.SetOnInteract(OnInteract);
                _bigButton.SetCanInteractAction(CanInteract);
                _bigButton.SetInteractionText(GetInteractionText);
                _bigButton.SetCanShowMassage(CanShowMessage);
            }
            else
            {
                Debug.LogWarning("[Button_Approve] BigButton is null; cannot interact.");
            }
            
            if (_glassCover)
            {
                _closedRot = _glassCover.localRotation;
                _openRot = _closedRot * Quaternion.Euler(_glassOpenAngle, 0f, 0f);
            }
        }

        public string GetInteractionText() => interactionText;

        public bool CanInteract()
        {
            return _isActive &&
                   _gameManager &&
                   _gameManager.CurrentGameplayState == GameplayState.Inspecting && _waveConfirmed && _keycodeConfirmed;
        }

        public bool CanShowMessage()
        {
            return _gameManager &&
                   _gameManager.CurrentGameplayState == GameplayState.Inspecting && _waveConfirmed && _keycodeConfirmed;
        }

        public void OnInteract()
        {
            if (!CanInteract())
                return;

            Debug.Log("[Button_Approve] Ship approved!");
            
            SetVisualState(false);

            _gameManager?.CompleteInspection(true);
            
            if (_buttonSoundString != "")
                AudioManager.Instance.PlaySFX(_buttonSoundString);
        }

        private void Activate()
        {
            _isActive = true;
            SetVisualState(true);
        }

        private void Deactivate()
        {
            _isActive = false;
            _waveConfirmed = false;
            _keycodeConfirmed = false;
            SetVisualState(false);
        }
        
        private void SetVisualState(bool isOn)
        {
            _indicatorLight?.SetLight(isOn);
            
            if (isOn)
                OpenGlassCover();
            else
                CloseGlassCover();
            
            if (_emissionMaterialInstance)
            {
                if (isOn)
                {
                    _emissionMaterialInstance.EnableKeyword("_EMISSION");
                    _emissionMaterialInstance.SetColor(_emissionProperty, _emissionColor);
                }
                else
                {
                    _emissionMaterialInstance.SetColor(_emissionProperty, Color.black);
                    _emissionMaterialInstance.DisableKeyword("_EMISSION");
                }
            }
        }
        
        private void OpenGlassCover()
        {
            if (_glassCover == null) return;

            if (_glassRoutine != null)
                StopCoroutine(_glassRoutine);

            _glassRoutine = StartCoroutine(AnimateGlassRoutine(_openRot));
        }

        private void CloseGlassCover()
        {
            if (_glassCover == null) return;

            if (_glassRoutine != null)
                StopCoroutine(_glassRoutine);

            _glassRoutine = StartCoroutine(AnimateGlassRoutine(_closedRot));
        }
        
        private IEnumerator AnimateGlassRoutine(Quaternion targetRotation)
        {
            yield return new WaitForSeconds(2f);
            
            Quaternion startRot = _glassCover.localRotation;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / _glassAnimationDuration;
                _glassCover.localRotation = Quaternion.Lerp(startRot, targetRotation, t);
                yield return null;
            }

            _glassCover.localRotation = targetRotation;
        }

    }
}
