using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public enum ETransactionUIState { income, expense, possibleExpense }
    public class TransactionElement : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private TextMeshProUGUI _reasonText;
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private Button _selectedButton;
        
        [Header("Settings")]
        [SerializeField] private Color _incomeColor = Color.blue;
        [SerializeField] private Color _expenseColor = Color.red;
        [SerializeField] private Color _possibleExpenseColor = Color.red;
        
        private Action _onSelectedCallback;
        
        private Transaction _transaction;
        private bool _isSelected = false;
        private Action _possibleTransactionCallback;
        
         private void Start()
        {
            _selectedButton.onClick.AddListener(OnSelectionButtonClicked);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            _transaction = default;
            _isSelected = false;
            _possibleTransactionCallback = null;
        }

        public void ShowPossibleTransaction(TransactionOption transactionOption)
        {
            _possibleTransactionCallback = transactionOption.OnExpenseMade;
            _transaction = transactionOption.Transaction;
            _isSelected = transactionOption.IsSelected;
            UpdateTexts(_transaction);
            
            if (_isSelected)
                UpdateColorAndSprites(ETransactionUIState.expense);
            else
                UpdateColorAndSprites(ETransactionUIState.possibleExpense);

            gameObject.SetActive(true);
            _selectedButton.gameObject.SetActive(true);
        }

        public void ShowTransaction(Transaction transaction)
        {
            _transaction = transaction;
            _isSelected = true;
            UpdateTexts(transaction);
            
            if(transaction.Type == ETransaction.income)
                UpdateColorAndSprites(ETransactionUIState.income);
            else 
                UpdateColorAndSprites(ETransactionUIState.expense);

            gameObject.SetActive(true);
            _selectedButton.gameObject.SetActive(false);
        }
        
        public int GetCurrencyAffect()
        {
            if(!_isSelected)
                return 0;
            
            int sign = _transaction.Type == ETransaction.income ? 1 : -1;
            return sign * _transaction.Amount;
        }
        
        public void SetSelectedCallback(Action callback)
        {
            _onSelectedCallback = callback;
        }

        public void TryFireExpenseCallback()
        {
            if(_isSelected)
                _possibleTransactionCallback?.Invoke();
        }

        private void UpdateTexts(Transaction transaction)
        {
            if (transaction.Type == ETransaction.income)
            {
                _reasonText.text = transaction.Reason;
                _amountText.text = transaction.Amount.ToString();
            }
            else
            {
                _reasonText.text = transaction.Reason;
                _amountText.text = '-' + transaction.Amount.ToString();
            }
        }

        private void UpdateColorAndSprites(ETransactionUIState state)
        {
            switch (state)
            {
                case ETransactionUIState.income:
                    _reasonText.color = _incomeColor;
                    _amountText.color = _incomeColor;
                    break;
                case ETransactionUIState.expense:
                    _reasonText.color = _expenseColor;
                    _amountText.color = _expenseColor;
                    break;
                case ETransactionUIState.possibleExpense:
                    _reasonText.color = _possibleExpenseColor;
                    _amountText.color = _possibleExpenseColor;
                    break;
            }
        }
        
        private void OnSelectionButtonClicked()
        {
            _isSelected = !_isSelected;
            _onSelectedCallback?.Invoke();
            
            if (_isSelected)
                UpdateColorAndSprites(ETransactionUIState.expense);
            else
                UpdateColorAndSprites(ETransactionUIState.possibleExpense);
        }
    }
}