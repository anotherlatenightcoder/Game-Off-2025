using System.Collections.Generic;

namespace Route24.GameOff
{
    public static class Transactions
    {
        public static Transaction GetApprovalSalary()
        {
            return new Transaction()
            {
                Type = ETransaction.income,
                Amount = ConstGameStats.ApprovedShipSalary,
                Reason = ConstStrings.Ship_APPROVE,
            };
        }
        
        public static Transaction GetWrongApprovalPenalty()
        {
            return new Transaction()
            {
                Type = ETransaction.expense,
                Amount = ConstGameStats.WrongApprovedPenalty,
                Reason = ConstStrings.Ship_APPROVE_PENALTY,
            };
        }

        public static Transaction GetSavings(int amount)
        {
            return new Transaction()
            {
                Type = ETransaction.income,
                Amount = amount,
                Reason = ConstStrings.Savings
            };
        }

        public static List<Transaction> ConvetToUIList(List<Transaction> transactions)
        {
            // Copy the list to not modify the original
            transactions = new List<Transaction>(transactions);

            // Find all Ship_APPROVE transactions
            var shipApproveList = transactions.FindAll(t => t.Reason == ConstStrings.Ship_APPROVE);

            if (shipApproveList.Count > 0)
            {
                // Remove all Ship_APPROVE transactions
                transactions.RemoveAll(t => t.Reason == ConstStrings.Ship_APPROVE);

                // Combine them into one
                int totalAmount = 0;

                foreach (var t in shipApproveList)
                {
                    totalAmount += t.Amount;
                }

                // Create the summary transaction
                var combined = new Transaction
                {
                    Type = ETransaction.income, // or shipApproveList[0].Type
                    Amount = totalAmount,
                    Reason = $"{ConstStrings.Ship_APPROVE} ({shipApproveList.Count})",
                };

                // Insert the combined transaction at the first original index
                transactions.Insert(1, combined);
            }

            return transactions;
        }

    }
}