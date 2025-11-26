using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Route24.Core;
using TMPro;
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
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private TextMeshProUGUI _currencyText;
        [SerializeField] private Button _nextDayButton;

        [Header("Asset References")] 
        [SerializeField] private TransactionElement _transactionPrefab;
        
        private List<TransactionElement> _transactionElements = new List<TransactionElement>();
        
        private GameManager _gameManager;
        private CurrencyManager _currencyManager;
        private EventHub _eventHub;
        private ShipManager _shipManager;
        
        public void Initialize()
        {
            _gameManager = ServiceLocator.Get<GameManager>();
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            _shipManager = ServiceLocator.Get<ShipManager>();
            
            _eventHub.Subscribe<DayEndedEvent>(OnDayEnded);
            _nextDayButton.onClick.AddListener(OnNextDayButtonClicked);
        }

        private void OnDestroy()
        {
            _eventHub.Unsubscribe<DayEndedEvent>(OnDayEnded);
        }

        private void OnDayEnded(DayEndedEvent dayEndedEvent)
        {
            StartCoroutine(DayEndeCoroutine(dayEndedEvent));
        }

        private IEnumerator DayEndeCoroutine(DayEndedEvent dayEndedEvent)
        {
            while (_shipManager.HasActiveShip) // wait for ship to exit
                yield return null;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            
            ShowTransactions();
            _dayText.text = $"Day {dayEndedEvent.Day}";
            
            _endDayPanel.SetActive(true);
        }
        
        private void OnNextDayButtonClicked()
        {
            Cursor.visible = false;
            _endDayPanel.SetActive(false);
            
            _currencyManager.ResetSavings(GetCurrency());
            HandleSelectedTransactionOptions();
            HideAllTransactions();
            
            _gameManager.StartNewDay();
        }

        #region Transactions
        private void ShowTransactions()
        {
            var transactions = _currencyManager.GetTransactions();                         // get transactions
            var uiTransactions = Transactions.ConvertToUIList(transactions);         // convert to UI list
            uiTransactions.AddRange(_currencyManager.GetRegularExpenses.ConvertToOptionsList()); // add regular expenses
            uiTransactions.AddRange(_currencyManager.GetExpenseOptions);                                 // add possible expenses
            
            SetTransactionUI(uiTransactions);
            UpdateCurrencyText();
        }

        private void SetTransactionUI(List<TransactionOption> transactions)
        {
            for (int i = 0; i < transactions.Count; i++)
            {
                if (i >= _transactionElements.Count) 
                    SpawnTransactionElement();
                
                TransactionOption optionTransaction = transactions[i];
                if(optionTransaction.IsPossibleExpense)
                    _transactionElements[i].ShowPossibleTransaction(optionTransaction);
                else
                    _transactionElements[i].ShowTransaction(transactions[i].Transaction);
            }
        }

        private void SpawnTransactionElement()
        {
            TransactionElement element = Instantiate(_transactionPrefab, _transactionHolder);
            element.SetSelectedCallback(UpdateCurrencyText);
            _transactionElements.Add(element);
        }

        private void HandleSelectedTransactionOptions()
        {
            foreach (TransactionElement element in _transactionElements)
                element.TryFireExpenseCallback();
        }
        
        private void HideAllTransactions()
        {
            foreach (TransactionElement element in _transactionElements)
                element.Hide();
        }
        #endregion
        
        private void UpdateCurrencyText()
        {
            _currencyText.text = $"${GetCurrency()}";
            
            _currencyText.transform.parent.SetAsLastSibling();
        }

        private int GetCurrency()
        {
            int currency = 0;
            
            foreach (TransactionElement element in _transactionElements)
                currency += element.GetCurrencyAffect();
            
            return currency;
        }
    }
}