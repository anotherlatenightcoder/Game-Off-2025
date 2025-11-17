using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class ExpenseManager : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 666;
        
        private CurrencyManager _currencyManager;
        private EventHub _eventHub;
        
        public void Initialize()
        {
            _currencyManager = ServiceLocator.Get<CurrencyManager>();
            _eventHub = ServiceLocator.Get<EventHub>();
            _eventHub.Subscribe<DayStartedEvent>(OnDayStarted);
        }

        private void OnDayStarted(DayStartedEvent eventData)
        {
            switch (eventData.Day)
            {
                case 1:
                    HandleDay1Upgrade();
                    return;
            }
        }

        private void HandleDay1Upgrade()
        {
            Transaction transaction = Transactions.Day1Upgrade();
           
            TransactionOption transactionOption = new TransactionOption(transaction, true, false, () =>
            {
                print("remove signal game");
                _currencyManager.RemovePossibleExpense(transaction);
            });
            
            _currencyManager.AddPossibleExpense(transactionOption);
        }
    }
}