using System.Collections.Generic;
using UnityEngine;

namespace Route24.GameOff
{
    [System.Serializable]
    public class ShipProfile
    {
        public string ShipName;
        public List<CargoItem> CargoList;
        public bool IsValid; // Whether this ship should be approved or not (for testing)
        public int ShipInspectionTime = 30;
        public GameObject ShipPrefab;
    }   
}
