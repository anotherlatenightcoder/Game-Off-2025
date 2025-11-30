using System.Collections;
using Route24.Core;
using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class CurrencyUI : MonoBehaviour, ISceneInitializable
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _currencyText;

        [Header("Flash Settings")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _gainColor = Color.green;
        [SerializeField] private Color _lossColor = Color.red;
        [SerializeField] private float _flashDuration = 2f;

        private CurrencyManager _currencyManager;
        private EventHub _eventHub;
        private Coroutine _flashRoutine;

        public void SceneInitialize()
        {
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _eventHub = ServiceLocator.Get<EventHub>();

            // Hide UI until tutorial is done
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;

            // Subscribe to tutorial completion
            _eventHub.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);

            // Subscribe to transaction updates
            _eventHub.Subscribe<TransactionAddedEvent>(OnTransactionAdded);

            // Initial update
            UpdateCurrencyText();
        }

        private void OnDestroy()
        {
            if (_eventHub == null) return;

            _eventHub.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
            _eventHub.Unsubscribe<TransactionAddedEvent>(OnTransactionAdded);
        }

        private void OnTutorialCompleted(TutorialCompletedEvent e)
        {
            // reveal UI
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        private void OnTransactionAdded(TransactionAddedEvent e)
        {
            // Update visible amount
            UpdateCurrencyText();

            // Flash effect
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            if (e.Transaction.Type == ETransaction.income)
                _flashRoutine = StartCoroutine(FlashRoutine(_gainColor));
            else
                _flashRoutine = StartCoroutine(FlashRoutine(_lossColor));
        }

        private void UpdateCurrencyText()
        {
            if (_currencyText)
                _currencyText.text = _currencyManager.CurrencyAmount.ToString();
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            _currencyText.color = flashColor;

            yield return new WaitForSeconds(_flashDuration);

            _currencyText.color = _defaultColor;

            _flashRoutine = null;
        }
    }
}
