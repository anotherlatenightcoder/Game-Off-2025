using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class CategoryHeaderUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        public void SetCategoryName(string category)
        {
            label.text = category;
        }
    }
}