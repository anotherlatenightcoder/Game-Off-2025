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
        public int Day;
    }
    
    [System.Serializable]
    public struct TransactionOption
    {
        public Transaction Transaction;

        public TransactionOption(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}