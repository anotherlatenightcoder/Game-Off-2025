using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Route24.GameOff
{
    public enum ETransactionUIState { income, expense, possibleExpense }
    public class TransactionElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _reasonText;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ShowTransaction(Transaction transaction)
        {
            _reasonText.text = transaction.Reason; 
            _amountText.text = transaction.Amount.ToString();

            gameObject.SetActive(true);
        }

        public int GetCurrencyAffect()
        {
            return int.Parse(_amountText.text);
        }
    }

}