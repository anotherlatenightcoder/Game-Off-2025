using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Route24.Core
{
    /// <summary>
    /// Ensures all registered systems initialize in a priority-based order.
    /// </summary>
    public class InitializationOrderManager : MonoBehaviour, IService
    {
        private readonly List<IInitializable> _systems = new();

        /// <summary>
        /// Adds a system to the InitManager to be run during startup
        /// </summary>
        /// <param name="system"></param>
        public void Register(IInitializable system)
        {
            if (system == null)
            {
                Debug.LogWarning("[InitManager] Trying to register null system.");
                return;
            }
            
            if (!_systems.Contains(system))
                _systems.Add(system);
        }

        /// <summary>
        /// Sorts systems by priority in ASC order, then runs initializes on them
        /// </summary>
        public void InitializeAll()
        {
            if (_systems.Count == 0)
            {
                Debug.Log("[InitManager] No systems registered for initialization.");
                return;
            }

            var ordered = _systems.OrderBy(s => s.InitializationPriority).ToList();
                
            Debug.Log($"[InitManager] Starting initialization of {ordered.Count} systems...");

            foreach (var system in ordered)
            {
                try
                {
                    system.Initialize();
                    Debug.Log($"[InitManager] SUCCESS {system.GetType().Name} initialized (priority {system.InitializationPriority}).");
                }
                catch (System.Exception e)
                {
                    // If the system cannot be initialized, should we remove it from the list?
                    Debug.LogError($"[InitManager] FAILED to initialize {system.GetType().Name}: {e}");
                }
            }
            
            Debug.Log("[InitManager] All systems initialized successfully.");
        }
        
        /// <summary>
        /// Clears the registration list (used on shutdown or reset).
        /// </summary>
        public void Clear()
        {
            _systems.Clear();
        }
    }   
}
