using System.Collections;
using Route24.Core;
using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class TubeClockController : MonoBehaviour, ISceneInitializable
    {
        [Header("Digit References")]
        [SerializeField] private TextMeshPro _digit1;
        [SerializeField] private TextMeshPro _digit2;
        [SerializeField] private TextMeshPro _digit3;
        [SerializeField] private TextMeshPro _digit4;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _activeColor = new(1f, 0.45f, 0.1f);
        [SerializeField] private Color _offColor = new(0.05f, 0.02f, 0f);
        [SerializeField] private float _flickerSpeed = 12f;
        [SerializeField] private float _flickerAmount = 0.25f;
        [SerializeField] private bool _enableFlicker = true;
        
        private bool _isActive = false;
        private int _remainingTime = 0;
        private Coroutine _timerRoutine;
        private Material[] _digitMaterials;
        private EventHub _eventHub;
        private GameManager _gameManager;
        
        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _gameManager = ServiceLocator.Get<GameManager>();

            _eventHub.Subscribe<DayEndedEvent>(e => ResetClock()); // Reset clock at end of day
            _eventHub.Subscribe<DayStartedEvent>(e => StartClock(ConstGameStats.DayTime)); // start clock at start of day
            // _eventHub.Subscribe<InspectionCompletedEvent>(e => StopClock(e.Approved, e.TimedOut));
            
            CacheDigitMaterials();
            UpdateDisplay(0);
        }
        
        private void Update()
        {
            if (!_enableFlicker || !_isActive || _digitMaterials == null)
                return;
            
            for (int i = 0; i < _digitMaterials.Length; i++)
            {
                if (_digitMaterials[i] == null) continue;

                float flicker = 1f + Mathf.Sin((Time.time + i * 0.5f) * _flickerSpeed) * _flickerAmount;
                Color flickerColor = Color.Lerp(_offColor, _activeColor, flicker);
                _digitMaterials[i].SetColor(ShaderUtilities.ID_FaceColor, flickerColor);
            }
        }
        
        private void CacheDigitMaterials()
        {
            _digitMaterials = new Material[4];
            if (_digit1) _digitMaterials[0] = _digit1.fontMaterial;
            if (_digit2) _digitMaterials[1] = _digit2.fontMaterial;
            if (_digit3) _digitMaterials[2] = _digit3.fontMaterial;
            if (_digit4) _digitMaterials[3] = _digit4.fontMaterial;
        }

        public void StartClock(int seconds)
        {
            if(seconds == -1) // no timer needed
                return;
            
            _remainingTime = seconds;
            _isActive = true;
            
            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);

            _timerRoutine = StartCoroutine(ClockCountdown());
        }
        
        public void StopClock(bool approved, bool timedOut)
        {
            _isActive = false;
            if (_timerRoutine != null)
                StopCoroutine(_timerRoutine);

            ResetClock();
        }

        public void ResetClock()
        {
            UpdateDisplay(0);
        }

        private IEnumerator ClockCountdown()
        {
            while (_remainingTime > 0)
            {
                UpdateDisplay(_remainingTime);
                yield return new WaitForSeconds(1f);
                _remainingTime--;
            }

            UpdateDisplay(0);
            _isActive = false;
            _gameManager.CompleteInspection(false, true);
        }
        
        private void UpdateDisplay(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            
            int minutes = seconds / 60;
            int secs = seconds % 60;
            
            minutes = Mathf.Clamp(minutes, 0, 99);

            string display = string.Format("{0:00}{1:00}", minutes, secs);

            // Now assign digits
            _digit1.text = display[0].ToString();
            _digit2.text = display[1].ToString();
            _digit3.text = display[2].ToString();
            _digit4.text = display[3].ToString();
        }
    }   
}
