using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Quotey.Core.Models;
using Quotey.Models;
using QuoteyCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public class QuotesService : IQuotesService
    {
        private AmazonDynamoDBClient _client;
        private Random _random;

        public QuotesService()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;
            _client = new AmazonDynamoDBClient(region);

            // Block until setup is done
            Task.WaitAll(setupTablesIfNotSetup());

            _random = new Random();
        }

        ~QuotesService()
        {
            _client.Dispose();
        }

        private async Task setupTablesIfNotSetup()
        {
            // Main quotes table.
            await DBUtils.CreateTableIfDoesNotExist(_client, DataDefinitions.QUOTES_TABLE,
                DataDefinitions.QUOTES_TABLE_HASH_KEY, true);

            // Motivation: to publish only approved quotes on the main quotes table quotey_quote.
            await DBUtils.CreateTableIfDoesNotExist(_client, DataDefinitions.QUOTES_PROPOSAL_TABLE,
                DataDefinitions.QUOTES_PROPOSAL_TABLE_HASH_KEY
                , false, DataDefinitions.QUOTES_PROPOSAL_TABLE_SORT_KEY, false, 
                DataDefinitions.QUOTES_PROPOSAL_TABLE_TTL);
        }

        #region quotes

        public async Task<Quote> GetRandomQuote()
        { 
            int randomQuoteId = _random.Next(1, (await getTableCount(DataDefinitions.QUOTES_TABLE)) + 1);
            return await GetQuoteById(randomQuoteId.ToString());
        }

        public async Task<List<Quote>> GetRandomQuotes(int amount)
        {
            int count = await getTableCount(DataDefinitions.QUOTES_TABLE);
            // To not exceed over limit of count
            int amountLeft = Math.Min(count, amount);

            HashSet<string> ids = new HashSet<string>(amountLeft);
            List<Dictionary<string, AttributeValue>> idsValuesToRequest = new List<Dictionary<string, AttributeValue>>();
            List<Quote> quotes = new List<Quote>(amountLeft);

            // Generate random Id that is not within list
            // Use amount to represent how much left
            while (amountLeft > 0)
            {
                string randId = (_random.Next(1, count + 1)).ToString();

                // Make sure it is not already fetched
                if (!ids.Contains(randId))
                {
                    // It is a long tree since batch item fetch can allow multiple keys
                    ids.Add(randId);
                    idsValuesToRequest.Add(new Dictionary<string, AttributeValue>
                    { {DataDefinitions.QUOTES_TABLE_HASH_KEY, new AttributeValue { N = randId } } });

                    amountLeft--;
                } 
            }

            return await getQuotesByIds(ids);
        }

        private async Task<int> getTableCount(string table)
        {
            DescribeTableResponse description = await _client.DescribeTableAsync(table);
            // The reason for the 2, at the start I only inserted 2 records and
            // since Dynamodb takes 6 hours to update the item count,
            // so it is useful for the first 6 hours of production and that is it.
            return description.Table.ItemCount == 0 ? 2 : (int) description.Table.ItemCount;
        }

        public async Task<Quote> GetQuoteById(string id)
        {
            List<Quote> quotes = await getQuotesByIds(new List<string>{ id});
            if (quotes == null || quotes.Count < 1)
                return null;

            return quotes[0];
        }

        private async Task<List<Quote>> getQuotesByIds(IEnumerable<string> ids)
        {
            // ETL
            List<Dictionary<string, AttributeValue>> listOfIdsToBeFetched 
                = new List<Dictionary<string, AttributeValue>>();
            foreach (string id in ids)
            {
                listOfIdsToBeFetched.Add(new Dictionary<string, AttributeValue>
                {
                    {DataDefinitions.QUOTES_TABLE_HASH_KEY, new AttributeValue{N = id} }
                });
            }

            BatchGetItemRequest request = new BatchGetItemRequest(new Dictionary<string, KeysAndAttributes>
            { {DataDefinitions.QUOTES_TABLE, new KeysAndAttributes { Keys =  listOfIdsToBeFetched } } });
            BatchGetItemResponse response = await _client.BatchGetItemAsync(request);

            // Extract data from the complex structure of response
            // Will return records
            List<Quote> quotes = new List<Quote>();
            foreach (Dictionary<string, AttributeValue> pair in response.Responses[DataDefinitions.QUOTES_TABLE])
            {
                quotes.Add(Quote.ToQuoteFromTable(pair));
            }

            return quotes;
        }

        #endregion

        #region quote proposals

        public async Task<string> SubmitQuote(QuoteWriteDTO quote)
        {
            string referenceId = Guid.NewGuid().ToString();
            // The best way to get accurate timing
            long ttlExpiryTimestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch)
                .Add(TimeSpan.FromHours(DataDefinitions.QUOTES_PROPOSAL_TABLE_TTL_VALUE_HOURS)).TotalSeconds;

            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>
            {
                {DataDefinitions.QUOTES_PROPOSAL_TABLE_HASH_KEY, new AttributeValue{ S = DateTime.UtcNow.ToString() } },
                {DataDefinitions.QUOTES_PROPOSAL_TABLE_SORT_KEY, new AttributeValue{ S = referenceId } },
                {"Text", new AttributeValue{ S = quote.Text } },
                {"Quoter", new AttributeValue{ S = quote.Quoter } },
                {"SubmitterEmail", new AttributeValue{ S = quote.SubmitterEmail } },
                {DataDefinitions.QUOTES_PROPOSAL_TABLE_TTL, new AttributeValue{ N = ttlExpiryTimestamp.ToString() } }
            };

            PutItemRequest request = new PutItemRequest
            {
                TableName = DataDefinitions.QUOTES_PROPOSAL_TABLE,
                Item = attributes
            };

            PutItemResponse response = await _client.PutItemAsync(request);
            return response.HttpStatusCode == HttpStatusCode.OK? referenceId: null;
        }

        #endregion
    }
}
