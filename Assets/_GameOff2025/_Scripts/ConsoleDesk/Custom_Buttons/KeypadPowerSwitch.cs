using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class KeypadPowerSwitch : GenericSwitch
    {
        [Header("Optional Light / Emission")]
        [SerializeField] protected Renderer _emissiveRenderer;
        [SerializeField] protected string _emissionColorProperty = "_EmissionColor";
        [SerializeField] protected Color _offEmissionColor = Color.black;
        [SerializeField] protected Color _onEmissionColor = Color.white;
        [SerializeField, Range(0f, 5f)] protected float _emissionIntensity = 1f;
        
        [Header("Audio")]
        [SerializeField] private string _buttonSoundString = "";
        
        private GameManager _gameManager;
        private Material _instanceMaterial;

        public override void SceneInitialize()
        {
            base.SceneInitialize();
            
            _gameManager = ServiceLocator.Get<GameManager>();
            
            if (_emissiveRenderer != null)
            {
                _instanceMaterial = new Material(_emissiveRenderer.material);
                _emissiveRenderer.material = _instanceMaterial;
            }
            
            SetEmission(IsActive);
            
            _eventHub?.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        }
        
        private void OnTutorialCompleted(TutorialCompletedEvent obj)
        {
            ForceOn(true);
            SetEmission(true);
        }

        public override bool CanInteract()
        {
            // Lets only allow toggling this in the tutorial (maybe we reset it each day?)
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial && TutorialController.Instance.TutorialStep2Completed)
                return true;
            
            return false;
        }

        protected override void OnToggledOn()
        {
            base.OnToggledOn();
            SetEmission(true);
            
            if (_buttonSoundString != "")
                AudioManager.Instance.PlaySFX(_buttonSoundString);
            
            _eventHub?.Publish(new KeypadPoweredOnEvent());
        }
        
        protected override void OnToggledOff()
        {
            base.OnToggledOff();
            SetEmission(false);
        }

        public override bool CanShowMessage()
        {
            return (_gameManager.CurrentGameplayState == GameplayState.Tutorial && !IsActive && TutorialController.Instance.TutorialStep2Completed);
        }
        
        protected void SetEmission(bool enabled)
        {
            if (_instanceMaterial == null)
                return;

            if (enabled)
            {
                Color final = _onEmissionColor * Mathf.LinearToGammaSpace(_emissionIntensity);
                _instanceMaterial.EnableKeyword("_EMISSION");
                _instanceMaterial.SetColor(_emissionColorProperty, final);
            }
            else
            {
                _instanceMaterial.SetColor(_emissionColorProperty, _offEmissionColor);
                _instanceMaterial.DisableKeyword("_EMISSION");
            }
        }
    }
}