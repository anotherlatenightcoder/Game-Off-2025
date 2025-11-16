using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class CurrencyManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 666;
        
        public int CurrencyAmount { get; private set; }

        private List<Transaction> _transactionsList = new(200);
        
        public void Initialize(){}
        public bool HasEnoughCurrency(int amount) => CurrencyAmount >=  amount; 
        
        public List<Transaction> GetTransactions() => _transactionsList;
        
        
        public void AddCurrency(int amount, string reason)
        {
            var transaction = new Transaction
            {
                Type = ETransaction.income,
                Amount = amount,
                Reason = reason,
                TimeStamp = GetTimeStamp()
            };
            
            CurrencyAmount += amount;
            _transactionsList.Add(transaction);
            Debug.Log($"[CurrencyManager] Added {transaction.Amount} to currency, new currency: {CurrencyAmount}");
        }

        public void RemoveCurrency(int amount, string reason)
        {
            var transaction = new Transaction
            {
                Type = ETransaction.expense,
                Amount = amount,
                Reason = reason,
                TimeStamp = GetTimeStamp()
            };
            
            CurrencyAmount -= transaction.Amount;
            _transactionsList.Add(transaction);
            Debug.Log($"[CurrencyManager] Removed {transaction.Amount} from currency, new currency: {CurrencyAmount}");
        }
        
        public bool TryRemoveCurrency(int amount, string reason)
        {
            if(!HasEnoughCurrency(amount))
                return false;
            
            RemoveCurrency(amount, reason);
            return true;
        }

        private TimeStamp GetTimeStamp()
        {
            return new TimeStamp(1);
        }
    }
}