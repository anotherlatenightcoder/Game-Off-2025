using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class ConsolePowerSwitch : GenericSwitch
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

        public override void SceneInitialize()
        {
            base.SceneInitialize();
            
            _gameManager = ServiceLocator.Get<GameManager>();
            
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
            if (_gameManager.CurrentGameplayState == GameplayState.Tutorial)
                return true;
            
            return false;
        }

        protected override void OnToggledOn()
        {
            base.OnToggledOn();
            SetEmission(true);
            
            if (_buttonSoundString != "")
                AudioManager.Instance.PlaySFX(_buttonSoundString);
            
            _eventHub?.Publish(new ConsolePoweredOnEvent());
        }
        
        protected override void OnToggledOff()
        {
            base.OnToggledOff();
            SetEmission(false);
        }

        public override bool CanShowMessage()
        {
            return (_gameManager.CurrentGameplayState == GameplayState.Tutorial && !IsActive);
        }
        
        protected void SetEmission(bool enabled)
        {
            if (_emissiveRenderer == null)
                return;

            // Important: Use material, NOT sharedMaterial (shared would change ALL)
            Material mat = _emissiveRenderer.material;

            if (enabled)
            {
                Color finalColor = _onEmissionColor * Mathf.LinearToGammaSpace(_emissionIntensity);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(_emissionColorProperty, finalColor);
            }
            else
            {
                mat.SetColor(_emissionColorProperty, _offEmissionColor);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }
}