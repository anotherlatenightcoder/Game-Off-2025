using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Route24.Core;

namespace Route24.GameOff
{
    /// <summary>
    /// Telemetry system taken from my Tiny Graves gamejam
    /// Added a few more tweaks to send interval updates
    /// Attempt to catch when the browser navigates away or closes
    /// I've removed _ from all private vars. This is starting to bother me
    /// </summary>
    public class WaveGameStats : MonoBehaviour
    {
        public static WaveGameStats Instance;
        
        [Header("Telemetry Settings")]
        [SerializeField] private bool enableTelemetry = true;
        [Tooltip("How often to send data (if any) in seconds")]
        [SerializeField] private float autoUploadInterval = 60f;
        [SerializeField] private TextMeshProUGUI sessionText;
        
        private const string apiUrl = "";
        private const string authToken = "";

        private string sessionId;
        private bool sessionStarted = false;
        private float lastAutoUploadTime = 0f;
        private bool hasUnsavedChanges = false;
        
        // Specific data we want to capture
        public DateTime sessionStartTimestamp;
        public float timePlayedSeconds = 0f;
        public bool tutorialSkipped = false;
        
        // Days 
        public int lastDayReached = 0;
        
        // Ship data
        public int shipsReceived = 0;
        public int shipsApproved = 0;
        public int shipsDeclined = 0;
        public int shipsSunk = 0;

        public int shipsCorrectApproved = 0;
        public int shipsIncorrectApproved = 0;

        public int shipsCorrectDeclined = 0;
        public int shipsIncorrectDeclined = 0;
        
        // Keypad
        public int keypadIncorrectAttempts = 0;
        
        // Cargo
        public int cargoScannedCount = 0;
        
        // Transactions
        public List<Dictionary<string, object>> transactions = new();

        // Upgrades
        public List<Dictionary<string, object>> upgradesBought = new();
        
        // Wave minigame
        public float timeInWaveMinigameSeconds = 0f;
        private bool trackingWaveTime = false;
        
        // Internal
        private bool recordTime = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            sessionStartTimestamp = DateTime.UtcNow;
            StartCoroutine(CreateSession());
        }

        private void Update()
        {
            if (!sessionStarted) return;

            if (recordTime)
                timePlayedSeconds += Time.deltaTime;

            if (trackingWaveTime)
                timeInWaveMinigameSeconds += Time.deltaTime;

            // Upload any new data once the timer has passed
            if (Time.time - lastAutoUploadTime > autoUploadInterval && hasUnsavedChanges)
            {
                UploadUpdate();
            }
        }

        // Public endpoints - stuff we call from other scripts,
        // however I'm still conflicted if we
        public void MarkTutorialSkipped()
        {
            tutorialSkipped = true;
            MarkDirty();
        }

        public void TrackShipReceived()
        {
            shipsReceived++;
            MarkDirty();
        }

        public void TrackShipOutcome(bool approved, bool correct)
        {
            if (approved) shipsApproved++;
            else shipsDeclined++;
            
            if (approved && correct) shipsCorrectApproved++;
            if (approved && !correct) shipsIncorrectApproved++;

            if (!approved && correct) shipsCorrectDeclined++;
            if (!approved && !correct) shipsIncorrectDeclined++;

            MarkDirty();
        }
        
        public void TrackKeypadIncorrect()
        {
            keypadIncorrectAttempts++;
            MarkDirty();
        }

        public void TrackCargoScanned()
        {
            cargoScannedCount++;
            MarkDirty();
        }
        
        public void TrackUpgrade(UpgradeData upgrade, int day)
        {
            upgradesBought.Add(new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "day", day },
                { "upgrade_baseid", upgrade.BaseId },
                { "upgrade_category", upgrade.Category },
                { "upgrade_name", upgrade.ExpenseName },
                { "upgrade_tier", upgrade.Tier },
                { "upgrade_cost", upgrade.Cost }
            });

            MarkDirty();
        }

        public void TrackTransaction(Transaction ta, int day)
        {
            transactions.Add(new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "day", day },
                { "type", ta.Type },
                { "amount", ta.Amount },
                { "reason", ta.Reason },
                { "ta_timestamp", ta.TimeStamp }
            });

            MarkDirty();
        }
        
        public void TrackShipSunk()
        {
            shipsSunk++;
            MarkDirty();
        }

        public void BeginWaveMinigame()
        {
            trackingWaveTime = true;
        }

        public void EndWaveMinigame()
        {
            trackingWaveTime = false;
        }

        public void MarkDayStart(int day)
        {
            lastDayReached = day;
            UploadEvent("day_start", day);
        }

        public void MarkDayEnd(int day)
        {
            lastDayReached = day;
            UploadEvent("day_end", day);
        }
        
        private void MarkDirty()
        {
            hasUnsavedChanges = true;
        }
        
        // Network requests
        private IEnumerator CreateSession()
        {
            if (!enableTelemetry) yield break;

            var payload = new Dictionary<string, object>
            {
                { "version", Application.version },
                { "platform", Application.platform.ToString() },
                { "unity_version", Application.unityVersion },
                { "device", SystemInfo.deviceModel },
                { "resolution", $"{Screen.width}x{Screen.height}" },
                { "session_start", sessionStartTimestamp.ToString("o") }
            };

            string json = JsonConvert.SerializeObject(payload);
            UnityWebRequest req = new UnityWebRequest($"{apiUrl}?action=start", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Game-Token", authToken);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                sessionId = resp["session_id"].ToString();
                sessionStarted = true;

                if (sessionText != null)
                    sessionText.text = sessionId;
            }
            else
            {
                Debug.LogError("Telemetry session start failed: " + req.error);
            }
        }
        
        private void UploadEvent(string label, int day)
        {
            var gameActions = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>
                {
                    { "timestamp", DateTime.UtcNow.ToString("o") },
                    { "event", label },
                    { "day", day }
                }
            };

            Upload(gameActions: gameActions);
        }

        public void UploadUpdate()
        {
            Upload();
        }
        
        private void Upload(Dictionary<string, object> gameStatus = null, List<Dictionary<string, object>> gameActions = null)
        {
            if (!enableTelemetry || string.IsNullOrEmpty(sessionId)) return;
            StartCoroutine(PostUpdate(gameStatus, gameActions));
        }
        
        private IEnumerator PostUpdate(Dictionary<string, object> gameStatus, List<Dictionary<string, object>> gameActions)
        {
            if (!sessionStarted) yield break;

            lastAutoUploadTime = Time.time;
            hasUnsavedChanges = false;

            var payload = BuildPayload();

            if (gameStatus != null) payload["game_status"] = gameStatus;
            if (gameActions != null) payload["game_actions"] = gameActions;

            string json = JsonConvert.SerializeObject(payload);

            UnityWebRequest req = new UnityWebRequest($"{apiUrl}?action=update", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Game-Token", authToken);

            yield return req.SendWebRequest();
        }
        
        private Dictionary<string, object> BuildPayload()
        {
            return new Dictionary<string, object>
            {
                { "session_id", sessionId },
                { "tutorial_skipped", tutorialSkipped },
                { "last_day", lastDayReached },

                { "ships_received", shipsReceived },
                { "ships_approved", shipsApproved },
                { "ships_declined", shipsDeclined },
                { "ships_correct_approved", shipsCorrectApproved },
                { "ships_incorrect_approved", shipsIncorrectApproved },
                { "ships_correct_declined", shipsCorrectDeclined },
                { "ships_incorrect_declined", shipsIncorrectDeclined },

                { "cargo_scanned", cargoScannedCount },
                { "keypad_incorrect", keypadIncorrectAttempts },

                { "transactions", transactions },
                { "upgrades", upgradesBought },

                { "ships_sunk", shipsSunk },

                { "time_played_seconds", Mathf.FloorToInt(timePlayedSeconds) },
                { "time_in_wave_seconds", Mathf.FloorToInt(timeInWaveMinigameSeconds) }
            };
        }
        
        // try catch onappquit
        void OnApplicationQuit()
        {
#if UNITY_WEBGL
            UploadEvent("quit", lastDayReached);
#endif
        }
    }
}
