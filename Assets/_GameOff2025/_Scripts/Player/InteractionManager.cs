using Route24.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _interactRange = 2.0f;
        [SerializeField] private LayerMask _interactableLayer;

        [Header("UI")]
        public Image crosshairDot;
        public TMPro.TextMeshProUGUI interactionText;

        private Camera _cam;
        private IInteractable _currentTarget;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (interactionText) interactionText.text = "";
        }

        private void Update()
        {
            if (GameManager.IsInFocusMode)
            {
                // Hide crosshair & text while in focus mode
                if (crosshairDot) crosshairDot.enabled = false;
                if (interactionText) interactionText.text = "";
                return;
            }
            
            DetectInteractable();

            if (_currentTarget != null && _currentTarget.CanInteract() && Input.GetKeyDown(KeyCode.E))
            {
                _currentTarget.OnInteract();
            }
        }

        private void DetectInteractable()
        {
            _currentTarget = null;
            if (interactionText) interactionText.text = "";
            
            Debug.DrawLine(
                _cam.transform.position,
                _cam.transform.position + _cam.transform.forward * _interactRange,
                Color.cyan
            );

            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out RaycastHit hit, _interactRange, _interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    _currentTarget = interactable;
                    
                    if (crosshairDot) crosshairDot.enabled = true;
                    
                    if (interactionText && _currentTarget.CanShowMessage())
                        interactionText.text = interactable.GetInteractionText();
                }
            }
        }
    }
}