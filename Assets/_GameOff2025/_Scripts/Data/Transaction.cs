namespace Route24.GameOff
{
    public enum ETransaction {income, expense}
    
    public struct Transaction
    {
        public ETransaction Type;
        public int Amount;
        public string Reason;
        public TimeStamp TimeStamp;
    }
}