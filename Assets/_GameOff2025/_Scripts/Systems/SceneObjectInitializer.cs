using System.Collections.Generic;
using UnityEngine;

namespace Route24.Core
{
    /// <summary>
    /// Initializes in-scene objects that depend on ServiceLocator or other runtime systems.
    /// Add this component to your scene and drag in the references that must be initialized after bootstrap.
    /// </summary>
    public class SceneObjectInitializer : MonoBehaviour
    {
        [Header("Scene Initializable Objects")]
        [Tooltip("Objects that implement ISceneInitializable and must be initialized after services are ready.")]
        [SerializeField] private List<MonoBehaviour> _initializables = new();

        public void InitializeAllSceneObjects()
        {
            if (_initializables == null || _initializables.Count == 0)
            {
                Debug.LogWarning("[SceneObjectInitializer] No objects assigned to initialize.");
                return;
            }

            Debug.Log($"[SceneObjectInitializer] Initializing {_initializables.Count} scene objects...");

            foreach (var obj in _initializables)
            {
                if (obj == null)
                {
                    Debug.LogWarning("[SceneObjectInitializer] Null reference in list — skipping.");
                    continue;
                }

                if (obj is ISceneInitializable init)
                {
                    try
                    {
                        init.SceneInitialize();
                        Debug.Log($"[SceneObjectInitializer] Initialized: {obj.name}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[SceneObjectInitializer] Failed to initialize {obj.name}: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SceneObjectInitializer] {obj.name} does not implement ISceneInitializable — skipping.");
                }
            }

            Debug.Log("[SceneObjectInitializer] Initialization complete.");
        }
    }
}
