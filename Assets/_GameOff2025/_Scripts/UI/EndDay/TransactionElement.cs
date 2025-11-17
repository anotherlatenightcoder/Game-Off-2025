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
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowPossibleTransaction(Transaction transaction, bool selected = false)
        {
            UpdateTexts(transaction);
            
            if (selected)
                UpdateColorAndSprites(ETransactionUIState.expense);
            else
                UpdateColorAndSprites(ETransactionUIState.income);

            gameObject.SetActive(true);
            _selectedButton.gameObject.SetActive(true);
        }

        public void ShowTransaction(Transaction transaction)
        {
            UpdateTexts(transaction);
            
            if(transaction.Type == ETransaction.income)
                UpdateColorAndSprites(ETransactionUIState.income);
            else 
                UpdateColorAndSprites(ETransactionUIState.expense);

            gameObject.SetActive(true);
            _selectedButton.gameObject.SetActive(false);
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
    }
}