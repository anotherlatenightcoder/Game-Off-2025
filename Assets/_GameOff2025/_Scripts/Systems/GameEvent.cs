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
    /// 1. Fired when the players presses the Inspection switch
    /// </summary>
    public struct Tutorial_OnInspectionStartedEvent { }
    
    /// <summary>
    /// 2. Fired when the players enters the cargo manifest
    /// </summary>
    public struct Tutorial_OnCargoFocusedEvent { }
    
    /// <summary>
    /// 3. Fired when the players scans a cargo entry
    /// </summary>
    public struct Tutorial_OnCargoScannedEvent { }
    
    /// <summary>
    /// 4. Fired when the player turns on the main console / power switch.
    /// </summary>
    public struct Tutorial_ConsolePoweredOnEvent { }
    
    /// <summary>
    /// 5. Fired when we focus the oscillator
    /// </summary>
    public struct Tutorial_ScopeFocusEvent { }
    
    /// <summary>
    /// 6. Fired when the oscilloscope tutorial calibration test completes.
    /// </summary>
    public struct Tutorial_ScopeCalibrationCompleteEvent { }
    
    /// <summary>
    /// 7. Fired when the player turns on the keypad.
    /// </summary>
    public struct Tutorial_KeypadPoweredOnEvent { }
    
    /// <summary>
    /// 8. Fired when any 4-digit test code is entered during the tutorial.
    /// </summary>
    public struct Tutorial_KeypadTestEnteredEvent { }
    
    /// <summary>
    /// 9. Fired when when we approve/decline the ship
    /// </summary>
    public struct Tutorial_ShipApproveDeclineEvent { }
    
    /// <summary>
    /// 10. Fired when we trigger the light switch.
    /// </summary>
    public struct LightsPoweredOnEvent
    {
        public bool Instant;

        public LightsPoweredOnEvent(bool instant)
        {
            Instant = instant;   
        }
    }

    /// <summary>
    /// 11. Fired when the player pulls the lever to open the docking gates and start Day 1.
    /// </summary>
    public struct Tutorial_DockGatesOpenedEvent { }
    
    /// <summary>
    /// Fired when the player pulls the lever to open the docking gates and start Day 1.
    /// </summary>
    public struct TutorialCompletedEvent { }

    // ───────────────────────────────
    // Game-related events
    // ───────────────────────────────
    
    /// <summary>
    /// Fired after the player has aligned their signal with the ship for the hold duration
    /// </summary>
    public struct WaveMatchedEvent { }
    
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
    
    /// <summary>
    /// Fired whenever we receive or deduct money
    /// </summary>
    public struct TransactionAddedEvent
    {
        public Transaction Transaction;

        public TransactionAddedEvent(Transaction t)
        {
            Transaction = t;
        }
    }
    
    /// <summary>
    /// Fired once the user entered 4 digits that match.
    /// </summary>
    public struct InspectionKeypadCodeMatchedEvent { }

    public struct InspectionCompletedEvent
    {
        public bool Approved;
        public bool Valid;
        public bool TimedOut;
        public bool PlayerTriggered;
        public ShipProfile Ship;

        public InspectionCompletedEvent(bool approved, bool valid, bool timedOut, ShipProfile ship, bool playerTriggered)
        {
            Approved = approved;
            Valid = valid;
            TimedOut = timedOut;
            Ship = ship;
            PlayerTriggered = playerTriggered;
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

    public struct SfxVolumeChangedEvent
    {
        public float Volume;

        public SfxVolumeChangedEvent(float volume)
        {
            Volume = volume;
        }
    }
}