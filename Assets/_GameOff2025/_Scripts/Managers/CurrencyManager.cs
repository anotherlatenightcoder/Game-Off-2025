using System;
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
        private List<Transaction> _regularExpenseList = new();
        private List<TransactionOption> _expenseOptions = new(); // rent, upgrades
        
        public bool HasEnoughCurrency(int amount) => CurrencyAmount >=  amount; 
        public List<Transaction> GetTransactions() => _transactionsList;
        public List<Transaction> GetRegularExpenses => _regularExpenseList;
        public List<TransactionOption> GetExpenseOptions => _expenseOptions;
        
        
        EventHub _eventHub;

        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub.Subscribe<InspectionCompletedEvent>(OnInspectionCompleted);
        }

        private void OnDestroy()
        {
            _eventHub.Unsubscribe<InspectionCompletedEvent>(OnInspectionCompleted);
        }
        
        public void AddTransaction(Transaction transaction)
        {
            _transactionsList.Add(transaction);
            if(transaction.Type == ETransaction.income)
                CurrencyAmount += transaction.Amount;
            else
                CurrencyAmount -= transaction.Amount;
        }

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

        public void AddRegularExpenses(Transaction transaction) => _regularExpenseList.Add(transaction);
        public void AddPossibleExpense(TransactionOption transactionOption) => _expenseOptions.Add(transactionOption);

        public void RemoveRegularExpenses(Transaction transaction)
        {
            for (int i = 0; i < _regularExpenseList.Count; i++)
                if (string.Equals(_regularExpenseList[i].Reason, transaction.Reason))
                    _regularExpenseList.RemoveAt(i);
        } 
        public void RemovePossibleExpense(Transaction transaction)
        { 
            for (int i = 0; i < _expenseOptions.Count; i++)
                if(string.Equals(_expenseOptions[i].Transaction.Reason, transaction.Reason))
                    _expenseOptions.RemoveAt(i);
        } 
        
        public void ResetSavings(int amount)
        {
            CurrencyAmount = amount;
            _transactionsList.Clear();
            _transactionsList.Add(Transactions.GetSavings(CurrencyAmount));
        }
        
        private void OnInspectionCompleted(InspectionCompletedEvent eventData)
        {
            if(eventData.Approved)
                if(eventData.Ship.IsValid)
                    AddTransaction(Transactions.GetApprovalSalary());
                else
                    AddTransaction(Transactions.GetWrongApprovalPenalty());
        }

        private TimeStamp GetTimeStamp()
        {
            return new TimeStamp(1);
        }
    }
}