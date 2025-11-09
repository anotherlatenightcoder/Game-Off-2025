using UnityEngine;

namespace Route24.GameOff
{
    [CreateAssetMenu(fileName = "ShipProfile", menuName = "Route24/Ship Profile", order = 1)]
    public class ShipProfileSO : ScriptableObject
    {
        public ShipProfile Profile;
    }
}