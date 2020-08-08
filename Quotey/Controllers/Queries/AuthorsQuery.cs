using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Controllers.Queries
{
    public class AuthorsQuery
    {
        // Amount = size of page
        private static int MAX_AMOUNT = 10;
        private static int DEFAULT_AMOUNT = 1; // 1 because is shared with many other requests
        private int amount = DEFAULT_AMOUNT;

        [NotNull]
        public int Amount
        {
            get
            {
                return amount;
            }
            set
            {
                // No zero
                if (value == 0)
                    amount = DEFAULT_AMOUNT;

                // Ignore negatives
                int absVal = Math.Abs(value);

                amount = Math.Min(MAX_AMOUNT, absVal);
            }
        }
    }
}
