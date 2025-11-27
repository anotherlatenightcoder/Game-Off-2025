using Route24.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class TutorialUIController : MonoBehaviour
    {
        [Header("Skip Tutorial UI")]
        [SerializeField] private CanvasGroup _skipCanvas;
        [SerializeField] private TextMeshProUGUI _skipText;
        [SerializeField] private Image _skipFillBar;
        
        [Header("Checklist UI")]
        [SerializeField] private CanvasGroup _checklistCanvas;
        
        [Header("Step Details Panel")]
        [SerializeField] private CanvasGroup _detailsCanvas;
        [SerializeField] private TextMeshProUGUI _detailsText;
        
        [Header("Tutorial Steps")]
        [SerializeField] private TutorialStepUI[] _steps;
        

        private void Awake()
        {
            ClearAll();
        }
        
        public void ClearAll()
        {
            // Hide canvases
            _checklistCanvas.alpha = 0f;
            _detailsCanvas.alpha = 0f;
            _skipCanvas.alpha = 0f;

            // Clear all checklist text
            foreach (var step in _steps)
            {
                if (step.checklistText != null)
                {
                    step.checklistText.text = "";
                    step.checklistText.alpha = 1f;
                    step.checklistText.fontStyle &= ~FontStyles.Strikethrough;
                }
            }

            _detailsText.text = "";
        }
        
        public void ShowStep(int index)
        {
            if (TutorialController.Instance.IsStepCompleted(TutorialStep.LightsPoweredOn))
                return;
            
            if (index < 0 || index >= _steps.Length) return;

            var step = _steps[index];

            // Show canvases
            _checklistCanvas.alpha = 1f;
            _detailsCanvas.alpha = 1f;

            // Update checklist item
            step.checklistText.text = step.checklistLabel;
            step.checklistText.alpha = 1f;

            // Update explanation text
            _detailsText.text = step.explanation;
        }
        
        public void StrikeStep(int index)
        {
            if (index < 0 || index >= _steps.Length) return;

            var step = _steps[index];
            step.checklistText.fontStyle |= FontStyles.Strikethrough;
        }

        public void UpdateSkipFill(float amount)
        {
            _skipFillBar.fillAmount = amount;
        }

        public void HideSkipPrompt()
        {
            _skipCanvas.alpha = 0;
            
            if (_skipFillBar) _skipFillBar.fillAmount = 0;
        }
        
        public void HideChecklist()
        {
            _checklistCanvas.alpha = 0;
        }

        public void ShowSkip()
        {
            _skipCanvas.alpha = 1;
        }
    }   
}
