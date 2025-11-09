using UnityEngine;

namespace Route24.GameOff
{
    [CreateAssetMenu(fileName = "DayConfig", menuName = "Route24/Day Config", order = 2)]
    public class DayConfig : ScriptableObject
    {
        public ShipProfileSO[] ShipProfiles;

        [Range(0,6)]
        public int MinCargoCount = 3;
        [Range(0,12)]
        public int MaxCargoCount = 5;

        [Range(0, 10)]
        public int ValidPercentage = 8; // ships with valid documentation, they can be approved
        [Range(0,10)]
        public int TimedShipRatio = 2;  // ships with low oxygen, they must be approved or denied before time runs out
    }
}