using System.Collections;
using Route24.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Route24.GameOff
{
    public class ShipController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _moveDuration = 3f;
        [SerializeField] private float _sinkDuration = 5f;
        
        [Header("Bobbing Settings")]
        [SerializeField] private bool _enableBobbing = true;
        [SerializeField] private float _bobAmplitude = 0.05f;
        [SerializeField] private float _bobFrequency = 1.5f;
        [SerializeField] private float _bobRotation = 2f;

        private Transform _spawnPoint;
        private Transform _dockPoint;
        private Transform _exitPoint;
        private Transform _sinkPoint;
        private GameManager _gameManager;
        private ShipManager _shipManager;
        private Coroutine _bobRoutine;
        private bool _isBobbing = false;

        private void Start()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _shipManager = ServiceLocator.Get<ShipManager>();
        }

        public void Initialize(Transform spawn, Transform dock, Transform exit, Transform sink)
        {
            _spawnPoint = spawn;
            _dockPoint = dock;
            _exitPoint = exit;
            _sinkPoint = sink;
        }
        
        public void MoveToDock()
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(_dockPoint.position, _moveDuration, OnArrivedAtDock));
        }

        public void MoveToExit()
        {
            StopBobbing();
            StopAllCoroutines();
            StartCoroutine(ExitFlowRoutine());
            
            AudioManager.Instance.PlaySFX("SHIP_APPROVED");
        }

        public void Sink()
        {
            StopBobbing();
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(_sinkPoint.position, _sinkDuration, OnSunk));
            
            AudioManager.Instance.PlaySFX("SHIP_SINK");
        }
        
        public void Decline()
        {
            StopBobbing();
            StopAllCoroutines();
            StartCoroutine(DeclineRoutine());
            
            AudioManager.Instance.PlaySFX("SHIP_DECLINED");
        }
        
        private IEnumerator MoveRoutine(Vector3 target, float duration, System.Action onComplete)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
            onComplete?.Invoke();
        }
        
        private IEnumerator RotateRoutine(float angleY, float duration)
        {
            Quaternion startRot = transform.rotation;
            Quaternion endRot = startRot * Quaternion.Euler(0f, angleY, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            transform.rotation = endRot;
            
            if (_enableBobbing)
            {
                _bobRoutine = StartCoroutine(BobbingRoutine());
            }
        }
        
        private IEnumerator ExitFlowRoutine()
        {
            yield return RotateRoutine(-45f, 1f);
            yield return MoveRoutine(_exitPoint.position, _moveDuration, OnExited);
        }


        private void OnArrivedAtDock()
        {
            Debug.Log("[ShipController] Arrived at dock, ready for inspection.");
            _shipManager.OnShipReadyForInspection(this);
            
            StopBobbing();
            StartCoroutine(RotateRoutine(45f, 1f));
            
            AudioManager.Instance.PlaySFX("SHIP_ARRIVED");
        }

        private void OnExited()
        {
            Debug.Log("[ShipController] Ship has left the dock.");
            Destroy(gameObject);
            _gameManager.OnShipExitComplete();
            
            AudioManager.Instance.PlaySFX("SHIP_EXIT");
        }

        private void OnSunk()
        {
            Debug.Log("[ShipController] Ship has sunk.");
            Destroy(gameObject);
            _gameManager.OnShipExitComplete();
        }

        private IEnumerator DeclineRoutine()
        {
            Debug.Log("[ShipController] Ship declined — returning to spawn.");
            
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(0f, 180f, 0f);

            float rotateDuration = 2f;
            float rotateElapsed = 0f;

            while (rotateElapsed < rotateDuration)
            {
                rotateElapsed += Time.deltaTime;
                float t = rotateElapsed / rotateDuration;
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.rotation = targetRot;
            
            yield return MoveRoutine(_spawnPoint.position, _moveDuration, OnDeclined);
        }

        private void OnDeclined()
        {
            Debug.Log("[ShipController] Ship returned to spawn (declined).");
            Destroy(gameObject);
            _gameManager.OnShipExitComplete();
        }
        
        private IEnumerator BobbingRoutine()
        {
            _isBobbing = true;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float time = Random.Range(0f, Mathf.PI * 2f);

            while (_isBobbing)
            {
                time += Time.deltaTime * _bobFrequency;
                float offset = Mathf.Sin(time) * _bobAmplitude;
                
                transform.position = startPos + Vector3.up * offset;
                
                float roll = Mathf.Sin(time * 0.5f) * _bobRotation;
                transform.rotation = startRot * Quaternion.Euler(0f, 0f, roll);

                yield return null;
            }
            
            transform.position = startPos;
            transform.rotation = startRot;
        }
        
        private void StopBobbing()
        {
            if (_bobRoutine != null)
            {
                _isBobbing = false;
                StopCoroutine(_bobRoutine);
                _bobRoutine = null;
            }
        }
    }   
}
