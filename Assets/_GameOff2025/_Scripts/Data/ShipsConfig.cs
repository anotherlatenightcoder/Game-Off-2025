using UnityEngine;

namespace Route24.GameOff
{
    [CreateAssetMenu(fileName = "ShipsOfDay", menuName = "Route24/Ships Of Day", order = 2)]
    public class ShipsConfig : ScriptableObject
    {
        public ShipProfileSO[] ShipProfiles;
    }
}