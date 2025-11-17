using System;
using System.Collections.Generic;
using Route24.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public class EndDayUI : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 16;
        
        [Header("Scene References")]
        [SerializeField] private GameObject _endDayPanel;
        [SerializeField] private Transform _transactionHolder;
        [SerializeField] private Button _nextDayButton;

        [Header("Asset References")] 
        [SerializeField] private TransactionElement _transactionPrefab;
        
        private List<TransactionElement> _transactionElements = new List<TransactionElement>();
        
        
        private GameManager _gameManager;
        private CurrencyManager _currencyManager;
        private EventHub _eventHub;
        
        public void Initialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            
            _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);
            _nextDayButton.onClick.AddListener(OnNextDayButtonClicked);
        }

        private void OnDestroy()
        {
            _eventHub.Unsubscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayEnded(DayEndedEvent dayEndedEvent)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            ShowTransactions();
            _endDayPanel.SetActive(true);
        }
        
        private void OnNextDayButtonClicked()
        {
            Cursor.visible = false;
            _endDayPanel.SetActive(false);
            HideAllTransactions();
            _gameManager.StartNewDay();
        }

        private void ShowTransactions()
        {
            var transactions = _currencyManager.GetTransactions();
            var uiTransactions = Transactions.ConvetToUIList(transactions);
            SetTransactionUI(uiTransactions);
        }

        private void SetTransactionUI(List<Transaction> transactions)
        {
            for (int i = 0; i < transactions.Count; i++)
            {
                if (i >= _transactionElements.Count) 
                    SpawnTransactionElement();
                
                _transactionElements[i].ShowTransaction(transactions[i]);
            }
        }

        
        private void SpawnTransactionElement()
        {
            TransactionElement element = Instantiate(_transactionPrefab, _transactionHolder);
            _transactionElements.Add(element);
        }
        
        [ContextMenu("Hide Transactions")]
        private void HideAllTransactions()
        {
            foreach (TransactionElement element in _transactionElements)
                element.Hide();
        }
    }
}