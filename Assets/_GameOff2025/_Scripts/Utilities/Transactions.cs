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

        public static Transaction Day1Upgrade()
        {
            return new Transaction()
            {
                Type = ETransaction.expense,
                Amount = 10,
                Reason = ConstStrings.Day1Upgrade
            };
        }

        public static List<TransactionOption> ConvertToUIList(List<Transaction> transactions)
        {
            // Copy the list to not modify the original
            var workingList = new List<Transaction>(transactions);

            // Find all Ship_APPROVE transactions
            var shipApproveList = workingList.FindAll(t => t.Reason == ConstStrings.Ship_APPROVE);

            if (shipApproveList.Count > 0)
            {
                // Remove all Ship_APPROVE transactions
                workingList.RemoveAll(t => t.Reason == ConstStrings.Ship_APPROVE);

                // Combine them into one
                int totalAmount = 0;

                foreach (var t in shipApproveList)
                    totalAmount += t.Amount;

                // Create the summary transaction
                var combined = new Transaction
                {
                    Type = ETransaction.income, // or shipApproveList[0].Type
                    Amount = totalAmount,
                    Reason = $"{ConstStrings.Ship_APPROVE} ({shipApproveList.Count})",
                };

                // Insert the combined transaction at the first original index
                
                if(workingList.Count >=1)
                    workingList.Insert(1, combined);
                else 
                    workingList.Add(combined);
            }

            // Convert to STransactionUI list
            var uiList = new List<TransactionOption>();

            foreach (var t in workingList)
            {
                uiList.Add(new TransactionOption(t)); // defaults: isPossibleExpense = false, isSelected = false
            }

            return uiList;
        }

    }
}