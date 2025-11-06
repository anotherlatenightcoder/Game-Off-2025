using UnityEngine;

namespace Route24.GameOff
{
    public class IndicatorLight : MonoBehaviour
    {
        [Header("Light Settings")]
        [SerializeField] private Light _lightSource;
        [SerializeField] private Color _emissionColor = new(1f, 0.3f, 0f);
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private float _emissionIntensity = 2f;
        [SerializeField] private float _flickerSpeed = 15f;
        [SerializeField] private float _flickerAmount = 0.2f;

        private Material _material;
        private bool _isOn = false;

        private void Awake()
        {
            if (_renderer)
                _material = _renderer.material;
            
            SetLight(false);
        }

        private void Update()
        {
            if (!_isOn || !_lightSource)
                return;
            
            float flicker = 1f + Mathf.Sin(Time.time * _flickerSpeed) * _flickerAmount;
            float intensity = Mathf.Max(0, _emissionIntensity * flicker);

            _lightSource.intensity = intensity;
            
            if (_renderer)
                _material.SetColor("_EmissionColor", _emissionColor * intensity);
        }

        public void SetLight(bool on)
        {
            _isOn = on;
            SetEmission(on);

            if (_lightSource)
                _lightSource.enabled = on;
        }

        private void SetEmission(bool on)
        {
            if (_material == null) return;

            if (on)
            {
                _material.EnableKeyword("_EMISSION");
                _material.SetColor("_EmissionColor", _emissionColor * _emissionIntensity);
            }
            else
            {
                _material.SetColor("_EmissionColor", Color.black);
                _material.DisableKeyword("_EMISSION");
            }
        }
    }
}