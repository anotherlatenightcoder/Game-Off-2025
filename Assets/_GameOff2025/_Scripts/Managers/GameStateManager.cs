using UnityEngine;

namespace Route24.Core
{
    public class GameStateManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 90;
        
        private EventHub _eventHub;
        
        private GameState _currentState = GameState.None;
        private GameState _previousState = GameState.None;

        public GameState CurrentState => _currentState;
        public GameState PreviousState => _previousState;
        
        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub?.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            ChangeState(GameState.Initializing);
            Debug.Log("[GameStateManager] Loaded and set to Initializing.");
        }

        public void ChangeState(GameState newState)
        {
            if (newState == _currentState)
                return;
            
            var oldState = _currentState;
            _previousState = oldState;
            _currentState = newState;
            
            Debug.Log($"[GameStateManager] State changed: {oldState} → {_currentState}");
            _eventHub?.Publish(new GameStateChangedEvent(oldState, _currentState));
            
            // Should we call an OnExit and OnEnter on these states?
        }
        
        private void OnSceneLoaded(SceneLoadedEvent e)
        {
            if (_currentState == GameState.Loading)
            {
                Debug.Log($"[GameStateManager] Scene {e.SceneName} finished loading, switching to Gameplay.");
                ChangeState(GameState.Gameplay);
            }
        }
        
        /// <summary>
        /// Toggles between Paused and whatever the previous non-paused state was.
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Paused)
            {
                // Return to whatever state we were in before pausing
                ChangeState(_previousState != GameState.None ? _previousState : GameState.Gameplay);
            }
            else
            {
                // Only allow pause from states that can be paused
                if (_currentState == GameState.Gameplay ||
                    _currentState == GameState.Tutorial)
                {
                    ChangeState(GameState.Paused);
                }
            }
        }
    }   
}
