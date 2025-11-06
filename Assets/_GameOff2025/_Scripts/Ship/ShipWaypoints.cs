using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class ShipWaypoints : MonoBehaviour
    {
        [Header("Ship Waypoints")]
        public Transform ShipSpawn;
        public Transform ShipDock;
        public Transform ShipExit;
        public Transform ShipSink;

        private void Start()
        {
            var gameManager = ServiceLocator.Get<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[ShipSceneSetup] GameManager not found via ServiceLocator.");
                return;
            }

            gameManager.RegisterSceneWaypoints(ShipSpawn, ShipDock, ShipExit, ShipSink);
            Debug.Log("[ShipSceneSetup] Waypoints registered with GameManager.");
        }
    }   
}
