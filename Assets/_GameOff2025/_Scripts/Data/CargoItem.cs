using UnityEngine;

namespace Route24.GameOff
{
    [CreateAssetMenu(fileName = "CargoItem", menuName = "Route24/Cargo Item", order = 0)]
    public class CargoItem : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        [Header("Visuals")]
        [SerializeField] private Sprite _icon;

        [Header("Metadata (Optional)")]
        [TextArea] [SerializeField] private string _description;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;
    }   
}