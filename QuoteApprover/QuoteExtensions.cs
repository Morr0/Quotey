using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Http;
using Quotey.Core.Models;
using QuoteyCore.Data;
using System.Collections.Generic;

namespace QuoteApprover
{
    public class QuoteExtensions
    {
        // Does not take care of the ID
        public static Quote CreateQuoteFromQuoteProposal(Dictionary<string, AttributeValue> attribute)
        {
            return new Quote
            {
                Text = attribute["Text"].S,
                Quoter = attribute["Quoter"].S,
                DateCreated = attribute[DataDefinitions.QUOTES_PROPOSAL_TABLE_HASH_KEY].S,
                ReferenceId = attribute[DataDefinitions.QUOTES_PROPOSAL_TABLE_SORT_KEY].S,
                SubmitterEmail = attribute["SubmitterEmail"].S
            };
        }

        public static Dictionary<string, AttributeValue> AttributesOfQuote(Quote quote)
        {
            return new Dictionary<string, AttributeValue>
            {
                {"Id", new AttributeValue{ N = quote.Id.ToString() } },
                {"Text", new AttributeValue{ S = quote.Text } },
                {"Quoter", new AttributeValue{ S = quote.Quoter } },
                {"ReferenceId", new AttributeValue{ S = quote.ReferenceId } },
                {"SubmitterEmail", new AttributeValue{ S = quote.SubmitterEmail } },
                {"DateCreated", new AttributeValue{ S = quote.DateCreated } },
                {"DateApproved", new AttributeValue{ S = quote.DateApproved } },
            };
        }
    }
}
