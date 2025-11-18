using System;

namespace Route24.GameOff
{
    public enum ETransaction {income, expense}
    
    [System.Serializable]
    public struct Transaction
    {
        public ETransaction Type;
        public int Amount;
        public string Reason;
        public TimeStamp TimeStamp;
    }
    
    [System.Serializable]
    public struct TransactionOption
    {
        public Transaction Transaction;
        public Action OnExpenseMade; // fire when expense is made, upgrades etc
        public bool IsPossibleExpense;
        public bool IsSelected;

        public TransactionOption(Transaction transaction, bool isPossibleExpense = false, bool isSelected = false, Action onExpenseMade = null)
        {
            Transaction = transaction;
            IsPossibleExpense = isPossibleExpense;
            IsSelected = isSelected;
            OnExpenseMade = onExpenseMade;
        }
    }
}