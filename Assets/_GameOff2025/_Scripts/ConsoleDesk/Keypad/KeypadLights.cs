using System.Collections;
using UnityEngine;

namespace Route24.GameOff
{
    public class KeypadLights : MonoBehaviour
    {
        [Header("Light Renderers")]
        [SerializeField] private Renderer _readyLight;
        [SerializeField] private Renderer _errorLight;

        [Header("Emission Colors")]
        [SerializeField] private Color _readyColor = Color.green;
        [SerializeField] private Color _errorColor = Color.red;

        [Header("Intensity Settings")]
        [SerializeField] private float _onIntensity = 3f;
        [SerializeField] private float _flashIntensity = 5f;

        private Material _readyMat;
        private Material _errorMat;

        private const string EMISSION_PROP = "_EmissionColor";

        private Coroutine _autoOffRoutine;

        private void Awake()
        {
            if (_readyLight) _readyMat = CreateInstance(_readyLight);
            if (_errorLight) _errorMat = CreateInstance(_errorLight);

            SetOff();
        }

        private Material CreateInstance(Renderer r)
        {
            Material newMat = new Material(r.material);
            r.material = newMat;
            return newMat;
        }

        public void SetOff()
        {
            SetEmission(_readyMat, Color.black);
            SetEmission(_errorMat, Color.black);
        }

        public void SetReady(bool ready)
        {
            if (ready)
                SetEmission(_readyMat, _readyColor * _onIntensity);
            else
                SetEmission(_readyMat, Color.black);

            SetEmission(_errorMat, Color.black);
        }

        public void FlashGreen()
        {
            FlashLight(_readyMat, _readyColor);
        }

        public void FlashGreenStrong()
        {
            FlashLight(_readyMat, _readyColor, strong:true);
        }

        public void FlashRed()
        {
            FlashLight(_errorMat, _errorColor);
        }

        private void FlashLight(Material mat, Color baseColor, bool strong = false)
        {
            float intensity = strong ? _flashIntensity * 1.5f : _flashIntensity;
            
            SetEmission(mat, baseColor * intensity);
            
            if (_autoOffRoutine != null)
                StopCoroutine(_autoOffRoutine);

            _autoOffRoutine = StartCoroutine(AutoOffRoutine(2f));
        }

        private IEnumerator AutoOffRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetOff();
        }

        private void SetEmission(Material mat, Color emission)
        {
            if (mat == null) return;
            mat.SetColor(EMISSION_PROP, emission);
        }
    }
}
