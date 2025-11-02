using UnityEngine;
using UnityEngine.UI;

namespace Route24.Core
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;
        
        [Header("Settings")]
        [Tooltip("Name of the scene to load when you start the game")]
        [SerializeField] private string _startSceneName = "Sandbox";
        
        private LoadingManager _loadingManager;
        private GameStateManager _gameStateManager;

        private void Start()
        {
            _loadingManager = ServiceLocator.Get<LoadingManager>();
            _gameStateManager = ServiceLocator.Get<GameStateManager>();

            _startButton.onClick.AddListener(OnStartClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }
        
        private void OnDestroy()
        {
            _startButton.onClick.RemoveListener(OnStartClicked);
            _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            Debug.Log("[MainMenuUI] Start clicked — loading sandbox scene...");

            _gameStateManager.ChangeState(GameState.Loading);
            _loadingManager.LoadScene(_startSceneName);
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quit clicked — exiting game.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
