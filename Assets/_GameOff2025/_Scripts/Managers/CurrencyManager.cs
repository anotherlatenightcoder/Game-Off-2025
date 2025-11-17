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
        
        public bool HasEnoughCurrency(int amount) => CurrencyAmount >=  amount; 
        public List<Transaction> GetTransactions() => _transactionsList;
        
        EventHub _eventHub;

        public void Initialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub.Subscribe<DayStartedEvent>(OnDayStarted);
            _eventHub.Subscribe<InspectionCompletedEvent>(OnInspectionCompleted);
        }

        private void OnDestroy()
        {
            _eventHub.Unsubscribe<DayStartedEvent>(OnDayStarted);
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
        
        private void OnDayStarted(DayStartedEvent dayStartedEvent) // did this assuming we dont care about previous days except for the savings. i can update this if necessery
        {
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