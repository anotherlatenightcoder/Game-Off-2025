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

        [Header("Signal Strength Variation")]
        [SerializeField, Range(2f, 5f)] private float signalStrengthChangeIntervalMin = 5f;
        [SerializeField, Range(2f, 5f)] private float signalStrengthChangeIntervalMax = 10f;
        [SerializeField, Range(0.8f, 1.2f)] private float signalStrengthMin = 0.8f;
        [SerializeField, Range(0.8f, 1.5f)] private float signalStrengthMax = 1.2f;

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
        private float signalStrength = 1f;

        // Match logic
        private float currentMatchPercent;
        private float holdTimer;
        private bool isLocked;
        private bool canMatch = true;

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

            // Start random signal strength variation
            StartCoroutine(SignalStrengthRoutine());

            UpdateUIText();
        }

        // ─────────────────────────────────────────────
        // External Setters
        // ─────────────────────────────────────────────
        public void SetPlayerAmplitude(float value) => playerAmplitude = Mathf.Clamp(value, 0.05f, 1f);
        public void SetPlayerFrequency(float value) => playerFrequency = Mathf.Clamp(value, 0.5f, 10f);
        public void SetPlayerSpeed(float value) => playerSpeed = Mathf.Clamp(value, 0.1f, 10f);

        private void RegenerateNoiseMask()
        {
            if (visibilityMask == null || visibilityMask.Length != resolution)
                visibilityMask = new bool[resolution];

            for (int i = 0; i < resolution; i++)
                visibilityMask[i] = rand.NextDouble() < (signalIntegrity / 100f);
        }

        // ─────────────────────────────────────────────
        // Rendering
        // ─────────────────────────────────────────────
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (resolution < 2) return;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float centerY = height / 2f;
            float step = width / (resolution - 1);

            // Ship wave
            DrawWave(vh, width, height, centerY, step, shipColor, shipAmplitude * signalStrength, shipFrequency, shipTimeOffset, shipHasVariation, true);

            // Player wave
            DrawWave(vh, width, height, centerY, step, playerColor, playerAmplitude, playerFrequency, playerTimeOffset, false, false);
        }

        private void DrawWave(VertexHelper vh, float width, float height, float centerY, float step, Color color,
                              float amp, float freq, float timeOff, bool allowState, bool useNoise)
        {
            Vector2 prev = Vector2.zero;
            bool prevValid = false;
            Color32 dark = new Color32(0, 0, 0, 255);

            float ampUsed = allowState ? currentAmplitude * signalStrength : amp;
            bool flatline = allowState && currentState == SignalState.Flatline;

            for (int i = 0; i < resolution; i++)
            {
                bool showVertex = !useNoise || visibilityMask[i];
                float x = i * step;
                float y = flatline
                    ? centerY
                    : centerY + Mathf.Sin((x / width) * freq * Mathf.PI * 2f + timeOff) * ampUsed * height * 0.5f;

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
            if (!Application.isPlaying)
                return;
            
            if (shipHasVariation) HandleShipBehavior();

            shipTimeOffset += Time.deltaTime * shipSpeed;
            playerTimeOffset += Time.deltaTime * playerSpeed;

            if (Time.time >= nextNoiseTime)
            {
                nextNoiseTime = Time.time + noiseRefreshRate;
                RegenerateNoiseMask();
            }

            if (canMatch)
                CalculateMatch();

            UpdateUIText();
            SetVerticesDirty();
        }

        // ─────────────────────────────────────────────
        // Random Signal Strength Logic
        // ─────────────────────────────────────────────
        private IEnumerator SignalStrengthRoutine()
        {
            while (true)
            {
                float wait = Random.Range(signalStrengthChangeIntervalMin, signalStrengthChangeIntervalMax);
                yield return new WaitForSeconds(wait);

                signalStrength = Random.Range(signalStrengthMin, signalStrengthMax);
            }
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
                float shipY = Mathf.Sin(t * shipFrequency + shipTimeOffset) * currentAmplitude * signalStrength;
                float playerY = Mathf.Sin(t * playerFrequency + playerTimeOffset) * playerAmplitude;
                totalDiff += Mathf.Abs(shipY - playerY);
            }

            float avgDiff = totalDiff / resolution;
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
                canMatch = true;
            }
            else if (roll < 65)
            {
                currentState = SignalState.Fading;
                targetAmplitude = Random.Range(0.05f, shipAmplitude * 0.4f);
                canMatch = true;
            }
            else if (roll < 85)
            {
                currentState = SignalState.Flatline;
                canMatch = false; // stop matching
                StartCoroutine(EndStateAfter(flatlineDuration, true));
            }
            else
            {
                currentState = SignalState.Frozen;
                StartCoroutine(EndStateAfter(freezeDuration));
            }
        }

        private IEnumerator EndStateAfter(float duration, bool resumeMatch = false)
        {
            yield return new WaitForSeconds(duration);
            currentState = SignalState.Normal;
            if (resumeMatch)
                canMatch = true;
        }
    }
}
