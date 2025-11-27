using TMPro;
using UnityEngine;

namespace Route24.Core
{
    [System.Serializable]
    public struct TutorialStepUI
    {
        [Tooltip("The TMPro object in the scene.")]
        public TextMeshProUGUI checklistText;
        
        [Tooltip("The line item for the checklist.")]
        [TextArea] 
        public string checklistLabel;
        
        [Tooltip("The dialogue text that explains more about the checklist item.")]
        [TextArea] 
        public string explanation;
    }   
}