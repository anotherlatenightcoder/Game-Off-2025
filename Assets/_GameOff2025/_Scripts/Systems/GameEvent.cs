using System.Collections.Generic;
using Route24.GameOff;

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
    // Tutorial-related events
    // ───────────────────────────────
    
    /// <summary>
    /// Fires once the scene loads.
    /// </summary>
    public struct TutorialStartedEvent { }
    
    /// <summary>
    /// Fired when the player turns on the main console / power switch.
    /// </summary>
    public struct ConsolePoweredOnEvent { }

    /// <summary>
    /// Fired when the oscilloscope tutorial calibration test completes.
    /// </summary>
    public struct ScopeCalibrationCompleteEvent { }

    /// <summary>
    /// Fired when any 4-digit test code is entered during the tutorial.
    /// </summary>
    public struct KeypadTestEnteredEvent { }

    /// <summary>
    /// Fired when the player pulls the lever to open the docking gates and start Day 1.
    /// </summary>
    public struct DockGatesOpenedEvent { }

    // ───────────────────────────────
    // Game-related events
    // ───────────────────────────────
    public struct ShipArrivedForInspectionEvent
    {
        public int ShipIndex;
        public ShipProfile Ship;

        public ShipArrivedForInspectionEvent(int shipIndex, ShipProfile ship)
        {
            ShipIndex = shipIndex;
            Ship = ship;
        }
    }
    
    public struct InspectionStartedEvent
    {
        public ShipProfile Ship;
        public InspectionStartedEvent(ShipProfile ship)
        {
            Ship = ship;
        }
    }

    public struct InspectionCompletedEvent
    {
        public bool Approved;
        public bool TimedOut;
        public ShipProfile Ship;

        public InspectionCompletedEvent(bool approved, bool timedOut, ShipProfile ship)
        {
            Approved = approved;
            TimedOut = timedOut;
            Ship = ship;
        }
    }
    
    public struct BannedCargoGeneratedEvent
    {
        public IReadOnlyList<CargoItem> BannedItems;
        public int Day;

        public BannedCargoGeneratedEvent(int day, IReadOnlyList<CargoItem> bannedItems)
        {
            Day = day;
            BannedItems = bannedItems;
        }
    }

    public struct BannedCargoClearedEvent { }

    public struct LeverActivatedEvent { }
    public struct LeverDeactivatedEvent { }

    public struct DayStartedEvent
    {
        public int Day;
        public DayStartedEvent(int day) => Day = day;
    }

    public struct DayEndedEvent
    {
        public int Day;
        public DayEndedEvent(int day) => Day = day;
    }
    
    // ───────────────────────────────
    // Generic events
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