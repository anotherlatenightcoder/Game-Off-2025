using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Route24.GameOff
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIWaveformDualRenderer : Graphic
    {
        // ─────────────────────────────────────────────
        // Shared Settings
        // ─────────────────────────────────────────────
        [SerializeField, Range(100, 1000)] private int _resolution = 400;
        [SerializeField, Range(0.5f, 5f)] private float _thickness = 2f;
        [SerializeField] private float _waveSpeed = 1f;

        // ─────────────────────────────────────────────
        // Ship (Target) Wave
        // ─────────────────────────────────────────────
        [SerializeField, Range(0.05f, 1f)] private float _shipAmplitude = 0.3f;
        [SerializeField, Range(0.5f, 10f)] private float _shipFrequency = 2f;
        [SerializeField] private Color _shipColor = Color.cyan;
        [SerializeField] private bool _shipHasVariation = true;

        // ─────────────────────────────────────────────
        // Player Wave
        // ─────────────────────────────────────────────
        [SerializeField, Range(0.05f, 1f)] private float _playerAmplitude = 0.3f;
        [SerializeField, Range(0.5f, 10f)] private float _playerFrequency = 2f;
        [SerializeField] private Color _playerColor = Color.green;

        // ─────────────────────────────────────────────
        // Signal Integrity
        // ─────────────────────────────────────────────
        [Tooltip("0 = fully degraded, 100 = perfect signal")]
        [SerializeField, Range(0f, 100f)] private float _signalIntegrity = 100f;
        [SerializeField] private float _noiseRefreshRate = 0.15f;

        // ─────────────────────────────────────────────
        // Behavior Variations (Ship Only)
        // ─────────────────────────────────────────────
        [SerializeField, Range(5f, 20f)] private float _minEventInterval = 6f;
        [SerializeField, Range(10f, 30f)] private float _maxEventInterval = 15f;
        [SerializeField, Range(1f, 5f)] private float _fadeDuration = 2f;
        [SerializeField, Range(1f, 5f)] private float _flatlineDuration = 3f;
        [SerializeField, Range(1f, 5f)] private float _freezeDuration = 2f;

        // ─────────────────────────────────────────────
        // Signal Strength Variation
        // ─────────────────────────────────────────────
        [SerializeField, Range(1f, 20f)] private float _signalChangeIntervalMin = 5f;
        [SerializeField, Range(1f, 20f)] private float _signalChangeIntervalMax = 10f;
        [SerializeField, Range(0.8f, 1.2f)] private float _signalStrengthMin = 0.8f;
        [SerializeField, Range(0.8f, 1.5f)] private float _signalStrengthMax = 1.2f;

        // ─────────────────────────────────────────────
        // Matching System
        // ─────────────────────────────────────────────
        [SerializeField, Range(0f, 100f)] private float _matchThreshold = 90f;
        [SerializeField, Range(0.5f, 5f)] private float _holdDuration = 3f;
        [SerializeField, Range(0.2f, 2f)] private float _drainSpeed = 0.5f;
        [SerializeField] private TextMeshProUGUI _matchPercentText;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private Transform _signalLockedIndicator;

        // ─────────────────────────────────────────────
        // Internal State
        // ─────────────────────────────────────────────
        private float _timeOffset;
        private float _shipTimeOffset;
        private float _playerTimeOffset;
        private float _nextNoiseTime;
        private bool[] _visibilityMask;
        private System.Random _rand;

        private enum SignalState { Normal, Fading, Flatline, Frozen }
        private SignalState _currentState = SignalState.Normal;
        private float _stateChangeTime;
        private float _targetAmplitude;
        private float _currentAmplitude;
        private float _signalStrength = 1f;

        private float _currentMatchPercent;
        private float _holdTimer;
        private bool _isLocked;
        private bool _canMatch = true;
        private bool _signalsStopped = false;


        // ─────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            _rand = new System.Random();
            _visibilityMask = new bool[_resolution];
            RegenerateNoiseMask();

            _targetAmplitude = _shipAmplitude;
            _currentAmplitude = _shipAmplitude;

            if (_shipHasVariation)
                ScheduleNextState();

            StartCoroutine(AdjustSignalStrengthRoutine());
            UpdateUIText();
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────
        
        public void SetPlayerAmplitude(float value) =>
            _playerAmplitude = Mathf.Clamp(value, 0.05f, 1f);
        
        public void SetPlayerFrequency(float value) =>
            _playerFrequency = Mathf.Clamp(value, 0.5f, 10f);
        
        public void SetPlayerOffset(float value) =>
            _playerTimeOffset = Mathf.Clamp(value, -3, 3f);
        
        public void StopSignals()
        {
            _signalsStopped = true;

            _shipAmplitude = 0.05f;
            _shipFrequency = 0.5f;

            _playerAmplitude = 0.05f;
            _playerFrequency = 0.5f;

            _currentAmplitude = _shipAmplitude;
            _signalStrength = 1f;

            Debug.Log("[UIWaveform] Signals stopped — flatline.");
        }

        // ─────────────────────────────────────────────
        // Private Methods
        // ─────────────────────────────────────────────

        /// <summary>Generates a new noise visibility mask based on signal integrity.</summary>
        private void RegenerateNoiseMask()
        {
            if (_visibilityMask == null || _visibilityMask.Length != _resolution)
                _visibilityMask = new bool[_resolution];

            for (int i = 0; i < _resolution; i++)
                _visibilityMask[i] = _rand.NextDouble() < (_signalIntegrity / 100f);
        }

        /// <summary>Draws the ship and player waveform lines.</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_resolution < 2)
                return;

            // Ensure visibility mask is valid before rendering
            if (_visibilityMask == null || _visibilityMask.Length != _resolution)
                RegenerateNoiseMask();

            DrawWave(vh, _shipColor, _shipAmplitude * _signalStrength, _shipFrequency, _timeOffset + _shipTimeOffset, _shipHasVariation, true);
            DrawWave(vh, _playerColor, _playerAmplitude, _playerFrequency, _timeOffset + _playerTimeOffset, false, false);
        }

        /// <summary>Draws an individual waveform line.</summary>
        private void DrawWave(VertexHelper vh, Color color, float amplitude, float frequency, float timeOffset, bool allowState, bool useNoise)
        {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float centerY = height / 2f;
            float step = width / (_resolution - 1);

            Vector2 prev = Vector2.zero;
            bool prevValid = false;

            float ampUsed = allowState ? _currentAmplitude * _signalStrength : amplitude;
            bool flatline = allowState && _currentState == SignalState.Flatline;

            for (int i = 0; i < _resolution; i++)
            {
                bool showVertex = !useNoise || _visibilityMask[i];
                float x = i * step;
                float y = flatline
                    ? centerY
                    : centerY + Mathf.Sin((x / width) * frequency * Mathf.PI * 2f + timeOffset) * ampUsed * height * 0.5f;

                Vector2 point = new Vector2(x, y);

                if (!showVertex)
                {
                    prevValid = false;
                    continue;
                }

                if (i > 0 && prevValid)
                {
                    Vector2 dir = (point - prev).normalized;
                    Vector2 normal = new Vector2(-dir.y, dir.x);

                    Vector2 v1 = prev + normal * _thickness;
                    Vector2 v2 = prev - normal * _thickness;
                    Vector2 v3 = point + normal * _thickness;
                    Vector2 v4 = point - normal * _thickness;

                    int start = vh.currentVertCount;

                    vh.AddVert(v1, color, Vector2.zero);
                    vh.AddVert(v2, color, Vector2.zero);
                    vh.AddVert(v3, color, Vector2.zero);
                    vh.AddVert(v4, color, Vector2.zero);

                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start + 2, start + 1, start + 3);
                }

                prev = point;
                prevValid = showVertex;
            }
        }

        /// <summary>Main update loop for signal animation and behavior.</summary>
        private void Update()
        {
            if (!Application.isPlaying || _signalsStopped)
                return;

            //if (_shipHasVariation)  // this is making the game harder.
            //    HandleShipBehavior();

            _timeOffset += Time.deltaTime * _waveSpeed;

            if (_canMatch)
                CalculateMatch();

            UpdateUIText();
            SetVerticesDirty();
        }

        /// <summary>Updates text fields with current match and hold values.</summary>
        private void UpdateUIText()
        {
            if (_matchPercentText)
                _matchPercentText.text = $"Match: {_currentMatchPercent:0.0}%";

            if (_timerText)
            {
                float remaining = Mathf.Max(0f, _holdDuration - _holdTimer);
                _timerText.text = $"Hold: {remaining:0.0}s";
            }
        }

        /// <summary>Handles random ship state transitions (fading, flatline, frozen).</summary>
        private void HandleShipBehavior()
        {
            if (_currentState == SignalState.Frozen)
                return;

            if (_currentState == SignalState.Fading)
                _currentAmplitude = Mathf.Lerp(_currentAmplitude, _targetAmplitude, Time.deltaTime / _fadeDuration);
            else
                _currentAmplitude = Mathf.Lerp(_currentAmplitude, _shipAmplitude, Time.deltaTime * 1.5f);

            if (Time.time >= _stateChangeTime)
            {
                ChangeState();
                ScheduleNextState();
            }
        }

        /// <summary>Schedules the next random ship state event.</summary>
        private void ScheduleNextState()
        {
            float interval = Random.Range(_minEventInterval, _maxEventInterval);
            _stateChangeTime = Time.time + interval;
        }

        /// <summary>Changes the ship signal to a new random state.</summary>
        private void ChangeState()
        {
            int roll = Random.Range(0, 100);

            if (roll < 40)
            {
                _currentState = SignalState.Normal;
                _targetAmplitude = _shipAmplitude;
                _canMatch = true;
            }
            else if (roll < 65)
            {
                _currentState = SignalState.Fading;
                _targetAmplitude = Random.Range(0.05f, _shipAmplitude * 0.4f);
                _canMatch = true;
            }
            else if (roll < 85)
            {
                _currentState = SignalState.Flatline;
                _canMatch = false;

                if (_signalLockedIndicator)
                    _signalLockedIndicator.gameObject.SetActive(false);

                StartCoroutine(EndStateAfterDelay(_flatlineDuration, true));
            }
            else
            {
                _currentState = SignalState.Frozen;
                StartCoroutine(EndStateAfterDelay(_freezeDuration));
            }
        }

        /// <summary>Calculates how closely the player and ship signals align.</summary>
        private void CalculateMatch()
        {
            float totalDiff = 0f;

            for (int i = 0; i < _resolution; i++)
            {
                float t = (i / (float)_resolution) * Mathf.PI * 2f;
                float shipY = Mathf.Sin(t * _shipFrequency + _shipTimeOffset) * _currentAmplitude * _signalStrength;
                float playerY = Mathf.Sin(t * _playerFrequency + _playerTimeOffset) * _playerAmplitude;
                totalDiff += Mathf.Abs(shipY - playerY);
            }

            float avgDiff = totalDiff / _resolution;
            float maxPossibleDiff = Mathf.Max(0.001f, _currentAmplitude + _playerAmplitude);
            float normalized = Mathf.Clamp01(avgDiff / maxPossibleDiff);
            _currentMatchPercent = (1f - normalized) * 100f;

            if (_currentMatchPercent >= _matchThreshold)
            {
                _holdTimer += Time.deltaTime;
                if (_holdTimer >= _holdDuration && !_isLocked)
                {
                    _isLocked = true;
                    Debug.Log("[WaveMatch] Signal Locked!");

                    StopSignals();
                    if (_signalLockedIndicator)
                        _signalLockedIndicator.gameObject.SetActive(true);
                }
            }
            else
            {
                if (_isLocked && _signalLockedIndicator)
                    _signalLockedIndicator.gameObject.SetActive(false);

                _isLocked = false;
                _holdTimer = Mathf.Max(0f, _holdTimer - Time.deltaTime * _drainSpeed);
            }
        }

        // ─────────────────────────────────────────────
        // Coroutines
        // ─────────────────────────────────────────────

        /// <summary>Gradually varies the ship signal strength over time.</summary>
        private IEnumerator AdjustSignalStrengthRoutine()
        {
            while (true)
            {
                float wait = Random.Range(_signalChangeIntervalMin, _signalChangeIntervalMax);
                yield return new WaitForSeconds(wait);

                _signalStrength = Random.Range(_signalStrengthMin, _signalStrengthMax);
            }
        }

        /// <summary>Ends a temporary state (flatline/frozen) after a delay.</summary>
        private IEnumerator EndStateAfterDelay(float duration, bool resumeMatch = false)
        {
            yield return new WaitForSeconds(duration);
            _currentState = SignalState.Normal;
            if (resumeMatch)
                _canMatch = true;
        }
    }
}
