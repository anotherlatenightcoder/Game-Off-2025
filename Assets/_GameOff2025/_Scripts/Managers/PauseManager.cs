using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class PauseManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 98;
        public bool IsPaused => _isPaused;

        private EventHub _eventHub;
        private bool _isPaused = false;

        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();

            Debug.Log("[PauseSystem] Initialized");
        }

        private void Update()
        {
            // I think we might need to put some additional conditional logic in here about when we're allowed to pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        private void Pause()
        {
            if (_isPaused) return;

            _isPaused = true;
            
            // Below we need to freeze both timescale as well as publish an event,
            // since we have so much weird logic we will need to add this event to our controllers etc..
            Time.timeScale = 0f;
            
            _eventHub.Publish(new GamePausedEvent(true));
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[PauseSystem] Game Paused");
        }

        private void Resume()
        {
            if (!_isPaused) return;

            _isPaused = false;

            Time.timeScale = 1f;

            _eventHub.Publish(new GamePausedEvent(false));
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[PauseSystem] Game Resumed");
        }
    }
}