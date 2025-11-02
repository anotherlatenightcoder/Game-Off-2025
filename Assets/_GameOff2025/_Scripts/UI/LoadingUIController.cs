using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.Core
{
    /// <summary>
    /// Handles the Fade-in/out and progress bar during scene load
    /// </summary>
    public class LoadingUIController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private float _fadeDuration = 1f;

        private void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponentInChildren<CanvasGroup>();

            if (_progressSlider != null)
                _progressSlider.value = 0f;
            
            gameObject.SetActive(false);
        }

        public IEnumerator FadeIn()
        {
            gameObject.SetActive(true);
            yield return Fade(0f, 1f);
            _progressSlider.gameObject.SetActive(true);
        }
        
        public IEnumerator FadeOut()
        {
            _progressSlider.gameObject.SetActive(false);
            yield return Fade(1f, 0f);
            gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                yield return null;
            }
            
            _canvasGroup.alpha = to;
        }

        public void SetProgress(float value)
        {
            if (_progressSlider != null)
                _progressSlider.value = Mathf.Clamp01(value);
        }
    }   
}
