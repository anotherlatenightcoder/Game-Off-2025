using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class TutorialUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvas;
        [SerializeField] private TextMeshProUGUI _skipText;
        [SerializeField] private Image _skipFillBar;
        
        [Header("Checklist")]
        [SerializeField] private CanvasGroup _checklistCanvas;
        [SerializeField] private TextMeshProUGUI _step1Text;
        [SerializeField] private TextMeshProUGUI _step2Text;
        [SerializeField] private TextMeshProUGUI _step3Text;
        [SerializeField] private TextMeshProUGUI _step4Text;
        [SerializeField] private TextMeshProUGUI _step5Text;

        private bool _visible = false;

        private void Awake()
        {
            HideImmediate();
        }

        public void ShowSkipPrompt()
        {
            _visible = true;
            _canvas.alpha = 1;
            _canvas.interactable = false;
            _canvas.blocksRaycasts = false;
            
            _skipFillBar.fillAmount = 0;
        }

        /// <summary>
        /// Amount should be passed as a 0-1
        /// </summary>
        /// <param name="amount"></param>
        public void UpdateSkipFill(float amount)
        {
            if (!_visible) return;
            
            _skipFillBar.fillAmount = Mathf.Clamp01(amount);
        }

        public void HideSkipPrompt()
        {
            _visible = false;
            _canvas.alpha = 0;
            
            if (_skipFillBar) _skipFillBar.fillAmount = 0;
        }

        public void HideChecklist()
        {
            _checklistCanvas.alpha = 0;
        }

        private void HideImmediate()
        {
            HideChecklist();
            HideSkipPrompt();
        }
        
        public void ShowChecklist(string initialStep)
        {
            _checklistCanvas.alpha = 1f;

            ResetChecklistStyles();

            _step1Text.text = initialStep;
            _step1Text.alpha = 1f;

            _step2Text.alpha = 0f;
            _step3Text.alpha = 0f;
            _step4Text.alpha = 0f;
            _step5Text.alpha = 0f;
        }

        public void SetStepCompleted(int stepIndex)
        {
            switch (stepIndex)
            {
                case 1:
                    Strike(_step1Text);
                    FadeIn(_step2Text);
                    break;
                case 2:
                    Strike(_step2Text);
                    FadeIn(_step3Text);
                    break;
                case 3:
                    Strike(_step3Text);
                    FadeIn(_step4Text);
                    break;
                case 4:
                    Strike(_step4Text);
                    FadeIn(_step5Text);
                    break;
                case 5:
                    Strike(_step5Text);
                    break;
            }
        }

        public void UpdateStepText(int stepIndex, string newText)
        {
            switch (stepIndex)
            {
                case 1: _step1Text.text = newText; break;
                case 2: _step2Text.text = newText; break;
                case 3: _step3Text.text = newText; break;
                case 4: _step4Text.text = newText; break;
                case 5: _step5Text.text = newText; break;
            }
        }

        private void ResetChecklistStyles()
        {
            ResetStyle(_step1Text);
            ResetStyle(_step2Text);
            ResetStyle(_step3Text);
            ResetStyle(_step4Text);
            ResetStyle(_step5Text);
        }

        private void ResetStyle(TextMeshProUGUI txt)
        {
            txt.fontStyle &= ~FontStyles.Strikethrough;
            txt.alpha = 1f;
        }

        private void Strike(TextMeshProUGUI txt)
        {
            txt.fontStyle |= FontStyles.Strikethrough;
        }

        private void FadeIn(TextMeshProUGUI txt)
        {
            txt.alpha = 1f;
        }
    }   
}
