namespace Route24.Core
{
    // ───────────────────────────────
    // Scene-related events
    // ───────────────────────────────
    public struct SceneLoadingStartedEvent
    {
        public string SceneName;
        public SceneLoadingStartedEvent(string sceneName) => SceneName = sceneName;
    }

    public struct SceneLoadedEvent
    {
        public string SceneName;
        public SceneLoadedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    public struct SceneUnloadedEvent
    {
        public string SceneName;
        public SceneUnloadedEvent(string sceneName) => SceneName = sceneName;
    }

    // ───────────────────────────────
    // Player-related events
    // ───────────────────────────────
    public struct PlayerDiedEvent { }

    // ───────────────────────────────
    // Enemy-related events
    // ───────────────────────────────
    public struct EnemyKilledEvent
    {
        public int EnemyID;
        public EnemyKilledEvent(int enemyId) => EnemyID = enemyId;
    }

    // ───────────────────────────────
    // Level-related events
    // ───────────────────────────────
    public struct LevelCompletedEvent
    {
        public int LevelIndex;
        public LevelCompletedEvent(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }

    // ───────────────────────────────
    // Game-related events
    // ───────────────────────────────
    public struct GamePausedEvent
    {
        public bool IsPaused;
        public GamePausedEvent(bool isPaused) => IsPaused = isPaused;
    }

    public struct GameResumedEvent { }
    
    public struct GameStateChangedEvent
    {
        public GameState PreviousState;
        public GameState NewState;

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            PreviousState = previous;
            NewState = current;
        }
    }
}