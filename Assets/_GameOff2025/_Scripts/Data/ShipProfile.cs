using System.Collections.Generic;
using UnityEngine;

namespace Route24.GameOff
{
    [System.Serializable]
    public class ShipProfile
    {
        public string ShipName;
        public List<CargoItem> CargoList;
        public bool IsValid; // Whether this ship should be approved or not 
        public int ShipInspectionTime = 30; // if -1, no time limit
        public GameObject ShipPrefab;
        public string EntryCode = "0000";
    }   
}
