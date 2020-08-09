using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

namespace Quotey.Core.Models
{
    public class Quote
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Quoter { get; set; }

        public string SubmitterEmail { get; set; }

        public string DateCreated { get; set; }

        public string DateApproved { get; set; }

        public static Quote ToQuoteFromTable(Dictionary<string, AttributeValue> item)
        {
            return new Quote
            {
                Id = int.Parse(item["Id"].N),
                Text = item["Text"].S,
                Quoter = item["Quoter"].S,
                SubmitterEmail = item["SubmitterEmail"].S,
                DateCreated = item["SubmitterEmail"].S,
                DateApproved = item["SubmitterEmail"].S
            };
        }

    }

    public class QuoteReadDTO
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Quoter { get; set; }
    }
}
