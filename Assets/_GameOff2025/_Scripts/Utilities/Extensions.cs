using System;
using System.Collections.Generic;

namespace Route24.GameOff
{
    public static class Extensions
    {
        private static readonly System.Random _random = new System.Random();
        
        public static void SetRandomTrueValues(this Span<bool> span, int trueCount)
        {
            for (int i = 0; i < trueCount; i++)
            {
                int index;
                do
                {
                    index = _random.Next(span.Length);
                } while (span[index]); // pick again if already true

                span[index] = true;
            }
        }

        public static List<TransactionOption> ConvertToOptionsList(this List<Transaction> transactions)
        {
            List<TransactionOption> options = new List<TransactionOption>(transactions.Count);
            
            foreach (var t in transactions)
                options.Add(new TransactionOption(t));
            
            return options;
        }
    }
}