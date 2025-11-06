using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Route24.Core
{
    /// <summary>
    /// SceneBootstrapper initializes all the core systems and remains persistent across scene loads.
    /// Acts as the entry point.
    /// </summary>
    public class SceneBootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The first scene to load after initialization")]
        [SerializeField] private string _initialSceneName = "MainMenu";

        [Tooltip("List of core prefab systems to load")]
        [SerializeField] private List<GameObject> _managerPrefabs;
        
        [Tooltip("Skips scene loading when already in a non-bootstrap scene (for prototyping)")]
        [SerializeField] private bool _skipSceneLoadIfAlreadyActive = true;
        
        private static bool _initialized = false;
        private readonly List<GameObject> _managers = new List<GameObject>();

        private void Awake()
        {
            if (_initialized)
            {
                Debug.Log("[Bootstrapper] Already initialized, destroying duplicate instance.");
                Destroy(gameObject);
                return;
            }
            
            _initialized = true;
            DontDestroyOnLoad(gameObject);

            InitializeCoreSystems();
            LoadInitialScene();
        }

        /// <summary>
        /// Instantiates all prefabs in a controlled order
        /// </summary>
        private void InitializeCoreSystems()
        {
            Debug.Log("[Bootstrapper] Initializing core systems...");
            
            var initOrderGO = new GameObject("InitializationOrderManager");
            var initOrderManager = initOrderGO.AddComponent<InitializationOrderManager>();
            DontDestroyOnLoad(initOrderGO);
            ServiceLocator.Register(initOrderManager);
            
            foreach (var prefab in _managerPrefabs)
            {
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name;
                DontDestroyOnLoad(instance);

                var components = instance.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component is IService)
                    {
                        var type = component.GetType();
                        ServiceLocator.Register(type, component);
                        Debug.Log($"[Bootstrapper] Registered service: {type.Name}");   
                    }

                    if (component is IInitializable initializable)
                    {
                        initOrderManager.Register(initializable);
                    }
                }
            }
            
            initOrderManager.InitializeAll();

            Debug.Log("[Bootstrapper] Core systems initialized in priority order.");

            var sceneInitializer = FindFirstObjectByType<SceneObjectInitializer>();
            sceneInitializer?.InitializeAllSceneObjects();
        }
        
        private void LoadInitialScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (_skipSceneLoadIfAlreadyActive && 
                !string.IsNullOrEmpty(activeScene.name) && 
                activeScene.name != "_Persistent")
            {
                Debug.Log($"[Bootstrapper] Already in scene '{activeScene.name}', skipping initial scene load.");
                return;
            }
            
            var loadingManager = ServiceLocator.Get<LoadingManager>();
            
            if (loadingManager != null)
            {
                Debug.Log($"[Bootstrapper] Loading initial scene: {_initialSceneName}");
                loadingManager.LoadScene(_initialSceneName);
            }
            else
            {
                Debug.LogWarning("[Bootstrapper] No LoadingManager found in ServiceLocator.");
            }
        }

        private void OnApplicationQuit()
        {
            _initialized = false;
            ServiceLocator.Clear();
        }
    }   
}
