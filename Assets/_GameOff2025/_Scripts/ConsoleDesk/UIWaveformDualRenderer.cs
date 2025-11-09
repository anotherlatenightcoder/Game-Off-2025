using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Route24.GameOff
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIWaveformDualRenderer : Graphic
    {
        public enum SignalNoiseMode { TransparentGaps, BlackNoise }

        [Header("Shared Settings")]
        [SerializeField, Range(100, 1000)] private int resolution = 400;
        [SerializeField, Range(0.5f, 5f)] private float thickness = 2f;

        [Header("Ship (Target) Wave")]
        [SerializeField, Range(0.05f, 1f)] private float shipAmplitude = 0.3f;
        [SerializeField, Range(0.5f, 10f)] private float shipFrequency = 2f;
        [SerializeField, Range(0.1f, 10f)] private float shipSpeed = 2f;
        [SerializeField] private Color shipColor = Color.cyan;
        [SerializeField] private bool shipHasVariation = true;

        [Header("Player Wave")]
        [SerializeField, Range(0.05f, 1f)] private float playerAmplitude = 0.3f;
        [SerializeField, Range(0.5f, 10f)] private float playerFrequency = 2f;
        [SerializeField, Range(0.1f, 10f)] private float playerSpeed = 2f;
        [SerializeField] private Color playerColor = Color.green;

        [Header("Signal Integrity")]
        [Tooltip("0 = fully degraded, 100 = perfect signal")]
        [SerializeField, Range(0f, 100f)] private float signalIntegrity = 100f;
        [SerializeField] private SignalNoiseMode noiseMode = SignalNoiseMode.TransparentGaps;
        [SerializeField] private float noiseRefreshRate = 0.15f;

        [Header("Behavior Variations (Ship Only)")]
        [SerializeField, Range(5f, 20f)] private float minEventInterval = 6f;
        [SerializeField, Range(10f, 30f)] private float maxEventInterval = 15f;
        [SerializeField, Range(1f, 5f)] private float fadeDuration = 2f;
        [SerializeField, Range(1f, 5f)] private float flatlineDuration = 3f;
        [SerializeField, Range(1f, 5f)] private float freezeDuration = 2f;

        [Header("Matching System")]
        [SerializeField, Range(0f, 100f)] private float matchThreshold = 90f;
        [SerializeField, Range(0.5f, 5f)] private float holdDuration = 3f;
        [SerializeField, Range(0.2f, 2f)] private float drainSpeed = 0.5f;
        [SerializeField] private TextMeshProUGUI matchPercentText;
        [SerializeField] private TextMeshProUGUI timerText;

        private float shipTimeOffset;
        private float playerTimeOffset;
        private float nextNoiseTime;
        private bool[] visibilityMask;
        private System.Random rand;

        // Ship behavior
        private enum SignalState { Normal, Fading, Flatline, Frozen }
        private SignalState currentState = SignalState.Normal;
        private float stateChangeTime;
        private float targetAmplitude;
        private float currentAmplitude;

        // Match logic
        private float currentMatchPercent;
        private float holdTimer;
        private bool isLocked;

        protected override void Awake()
        {
            base.Awake();
            rand = new System.Random();
            visibilityMask = new bool[resolution];
            RegenerateNoiseMask();

            targetAmplitude = shipAmplitude;
            currentAmplitude = shipAmplitude;

            if (shipHasVariation)
                ScheduleNextState();

            UpdateUIText();
        }

        private void RegenerateNoiseMask()
        {
            if (visibilityMask == null || visibilityMask.Length != resolution)
                visibilityMask = new bool[resolution];

            for (int i = 0; i < resolution; i++)
                visibilityMask[i] = rand.NextDouble() < (signalIntegrity / 100f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (resolution < 2) return;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float centerY = height / 2f;
            float step = width / (resolution - 1);

            // --- Ship wave ---
            DrawWave(vh, width, height, centerY, step, shipColor, shipAmplitude, shipFrequency, shipTimeOffset, shipHasVariation, true);

            // --- Player wave ---
            DrawWave(vh, width, height, centerY, step, playerColor, playerAmplitude, playerFrequency, playerTimeOffset, false, false);
        }

        private void DrawWave(VertexHelper vh, float width, float height, float centerY, float step, Color color,
                              float amp, float freq, float timeOff, bool allowState, bool useNoise)
        {
            Vector2 prev = Vector2.zero;
            bool prevValid = false;
            Color32 dark = new Color32(0, 0, 0, 255);

            float ampUsed = allowState ? currentAmplitude : amp;
            bool flatline = allowState && currentState == SignalState.Flatline;

            for (int i = 0; i < resolution; i++)
            {
                bool showVertex = !useNoise || visibilityMask[i];
                float x = i * step;
                float y;

                if (flatline)
                    y = centerY;
                else
                {
                    float t = (x / width) * freq * Mathf.PI * 2f;
                    y = centerY + Mathf.Sin(t + timeOff) * ampUsed * height * 0.5f;
                }

                Vector2 point = new Vector2(x, y);

                if (!showVertex && noiseMode == SignalNoiseMode.TransparentGaps)
                {
                    prevValid = false;
                    continue;
                }

                if (i > 0 && prevValid)
                {
                    Vector2 dir = (point - prev).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);

                    Vector2 v1 = prev + normal * thickness;
                    Vector2 v2 = prev - normal * thickness;
                    Vector2 v3 = point + normal * thickness;
                    Vector2 v4 = point - normal * thickness;

                    int startIndex = vh.currentVertCount;
                    Color32 segmentColor = (showVertex || noiseMode == SignalNoiseMode.TransparentGaps)
                        ? (Color32)color : dark;

                    vh.AddVert(v1, segmentColor, Vector2.zero);
                    vh.AddVert(v2, segmentColor, Vector2.zero);
                    vh.AddVert(v3, segmentColor, Vector2.zero);
                    vh.AddVert(v4, segmentColor, Vector2.zero);

                    vh.AddTriangle(startIndex + 0, startIndex + 1, startIndex + 2);
                    vh.AddTriangle(startIndex + 2, startIndex + 1, startIndex + 3);
                }

                prev = point;
                prevValid = showVertex;
            }
        }

        private void Update()
        {
            // to fix the graphics ui rendering bug wiht constant scene redrawing
            if (!Application.isPlaying)
                return;
            
            HandlePlayerInput();
            if (shipHasVariation) HandleShipBehavior();

            shipTimeOffset += Time.deltaTime * shipSpeed;
            playerTimeOffset += Time.deltaTime * playerSpeed;

            if (Time.time >= nextNoiseTime)
            {
                nextNoiseTime = Time.time + noiseRefreshRate;
                RegenerateNoiseMask();
            }

            CalculateMatch();
            UpdateUIText();

            SetVerticesDirty();
        }

        // ─────────────────────────────────────────────
        // Matching Logic
        // ─────────────────────────────────────────────
        private void CalculateMatch()
        {
            float totalDiff = 0f;

            for (int i = 0; i < resolution; i++)
            {
                float t = (i / (float)resolution) * Mathf.PI * 2f;

                float shipY = Mathf.Sin(t * shipFrequency + shipTimeOffset) * currentAmplitude;
                float playerY = Mathf.Sin(t * playerFrequency + playerTimeOffset) * playerAmplitude;

                totalDiff += Mathf.Abs(shipY - playerY);
            }

            float avgDiff = totalDiff / resolution;

            // Normalize by possible amplitude difference range (worst case)
            float maxPossibleDiff = Mathf.Max(0.001f, currentAmplitude + playerAmplitude);
            float normalized = Mathf.Clamp01(avgDiff / maxPossibleDiff);
            currentMatchPercent = (1f - normalized) * 100f;

            if (currentMatchPercent >= matchThreshold)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdDuration && !isLocked)
                {
                    isLocked = true;
                    Debug.Log("[WaveMatch] Signal Locked!");
                }
            }
            else
            {
                isLocked = false;
                holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * drainSpeed);
            }
        }


        private void UpdateUIText()
        {
            if (matchPercentText)
                matchPercentText.text = $"Match: {currentMatchPercent:0.0}%";

            if (timerText)
            {
                float remaining = Mathf.Max(0f, holdDuration - holdTimer);
                timerText.text = $"Hold: {remaining:0.0}s";
            }
        }

        // ─────────────────────────────────────────────
        // Ship Behavior Logic
        // ─────────────────────────────────────────────
        private void HandleShipBehavior()
        {
            if (currentState == SignalState.Frozen)
                return;

            if (currentState == SignalState.Fading)
                currentAmplitude = Mathf.Lerp(currentAmplitude, targetAmplitude, Time.deltaTime / fadeDuration);
            else
                currentAmplitude = Mathf.Lerp(currentAmplitude, shipAmplitude, Time.deltaTime * 1.5f);

            if (Time.time >= stateChangeTime)
            {
                ChangeState();
                ScheduleNextState();
            }
        }

        private void ScheduleNextState()
        {
            float interval = Random.Range(minEventInterval, maxEventInterval);
            stateChangeTime = Time.time + interval;
        }

        private void ChangeState()
        {
            int roll = Random.Range(0, 100);
            if (roll < 40)
            {
                currentState = SignalState.Normal;
                targetAmplitude = shipAmplitude;
            }
            else if (roll < 65)
            {
                currentState = SignalState.Fading;
                targetAmplitude = Random.Range(0.05f, shipAmplitude * 0.4f);
            }
            else if (roll < 85)
            {
                currentState = SignalState.Flatline;
                StartCoroutine(EndStateAfter(flatlineDuration));
            }
            else
            {
                currentState = SignalState.Frozen;
                StartCoroutine(EndStateAfter(freezeDuration));
            }
        }

        private IEnumerator EndStateAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            currentState = SignalState.Normal;
        }

        // ─────────────────────────────────────────────
        // Player Input
        // ─────────────────────────────────────────────
        private void HandlePlayerInput()
        {
            // Amplitude W/S
            if (Input.GetKey(KeyCode.W))
                playerAmplitude = Mathf.Clamp(playerAmplitude + Time.deltaTime * 0.2f, 0.05f, 1f);
            if (Input.GetKey(KeyCode.S))
                playerAmplitude = Mathf.Clamp(playerAmplitude - Time.deltaTime * 0.2f, 0.05f, 1f);

            // Frequency Z/C
            if (Input.GetKey(KeyCode.Z))
                playerFrequency = Mathf.Clamp(playerFrequency - Time.deltaTime * 0.5f, 0.5f, 10f);
            if (Input.GetKey(KeyCode.C))
                playerFrequency = Mathf.Clamp(playerFrequency + Time.deltaTime * 0.5f, 0.5f, 10f);

            // Speed A/D
            if (Input.GetKey(KeyCode.A))
                playerSpeed = Mathf.Clamp(playerSpeed - Time.deltaTime * 0.5f, 0.1f, 10f);
            if (Input.GetKey(KeyCode.D))
                playerSpeed = Mathf.Clamp(playerSpeed + Time.deltaTime * 0.5f, 0.1f, 10f);
        }
    }
}
