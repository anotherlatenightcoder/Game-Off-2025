using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Route24.Core
{
    public class LoadingManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 50;
        
        [Header("References")]
        [SerializeField] private GameObject _loadingUIPrefab;

        private LoadingUIController _loadingUI;
        private EventHub _eventHub;
        
        public void Initialize()
        {
            InitializeUI();
            HookSceneEvents();
            
            _eventHub = ServiceLocator.Get<EventHub>();
        }

        private void InitializeUI()
        {
            if (_loadingUIPrefab == null)
            {
                Debug.LogError("[LoadingManager] Missing Loading UI Prefab.");
                return;
            }
            
            GameObject uiInstance = Instantiate(_loadingUIPrefab);
            _loadingUI = uiInstance.GetComponent<LoadingUIController>();
            DontDestroyOnLoad(uiInstance);
        }
        
        private void HookSceneEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// Loads a new scene async with fade
        /// </summary>
        /// <param name="sceneName"></param>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            if (_loadingUI == null)
            {
                Debug.LogError("[LoadingManager] Missing Loading UI Controller.");
                yield break;
            }
            
            _eventHub?.Publish(new SceneLoadingStartedEvent(sceneName));

            yield return _loadingUI.FadeIn();
            
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
            loadOp.allowSceneActivation = false;
            
            float progress = 0f;

            while (!loadOp.isDone)
            {
                progress = Mathf.Clamp01(loadOp.progress / 0.9f);
                
                _loadingUI.SetProgress(progress);

                if (progress >= 0.99f)
                    break;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            
            loadOp.allowSceneActivation = true;

            yield return null;
            
            yield return _loadingUI.FadeOut();
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[LoadingManager] Scene loaded: {scene.name}");
            _eventHub?.Publish(new SceneLoadedEvent(scene.name));
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"[LoadingManager] Scene unloaded: {scene.name}");
            _eventHub?.Publish(new SceneUnloadedEvent(scene.name));
        }
    }   
}
