using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Quotey.Core.Models;
using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public class QuotesService : IQuotesService
    {

        #region table definitions

        // Quotes table
        public static string QUOTES_TABLE = "quotey_quote";
        public static string QUOTES_TABLE_HASH_KEY = "Id";

        // Quotes proposals table
        public static string QUOTES_PROPOSAL_TABLE = "quotey_quote_proposal";
        public static string QUOTES_PROPOSAL_TABLE_HASH_KEY = "DateCreated";

        // Quotes authors table
        public static string QUOTES_AUTHORS_TABLE = "quotey_quote_author";
        public static string QUOTES_AUTHORS_TABLE_HASH_KEY = "Author";
        public static string QUOTES_AUTHORS_TABLE_QUOTES_IDS = "QuotesIds";

        #endregion

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
            await DBUtils.CreateTableIfDoesNotExist(_client, QUOTES_TABLE, QUOTES_TABLE_HASH_KEY, true);

            // Motivation: to not use GSI on other tables and worry about provisioning
            // for each new author, store a new record of hash key (Author) and
            // an array of quote primary keys assosciated with it (Quotes).
            await DBUtils.CreateTableIfDoesNotExist(_client, QUOTES_AUTHORS_TABLE, QUOTES_AUTHORS_TABLE_HASH_KEY);

            // Motivation: to publish only approved quotes on the main quotes table quotey_quote.
            await DBUtils.CreateTableIfDoesNotExist(_client, QUOTES_PROPOSAL_TABLE, QUOTES_PROPOSAL_TABLE_HASH_KEY);
            
        }

        #region quotes

        public async Task<Quote> GetRandomQuote()
        { 
            int randomQuoteId = _random.Next(1, (await getTableCount(QUOTES_TABLE)) + 1);
            return await GetQuoteById(randomQuoteId.ToString());
        }

        public async Task<List<Quote>> GetRandomQuotes(int amount)
        {
            int count = await getTableCount(QUOTES_TABLE);
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
                    { {QUOTES_TABLE_HASH_KEY, new AttributeValue { N = randId } } });

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
                    {QUOTES_TABLE_HASH_KEY, new AttributeValue{N = id} }
                });
            }

            BatchGetItemRequest request = new BatchGetItemRequest(new Dictionary<string, KeysAndAttributes>
            { {QUOTES_TABLE, new KeysAndAttributes { Keys =  listOfIdsToBeFetched } } });
            BatchGetItemResponse response = await _client.BatchGetItemAsync(request);

            // Extract data from the complex structure of response
            // Will return records
            List<Quote> quotes = new List<Quote>();
            foreach (Dictionary<string, AttributeValue> pair in response.Responses[QUOTES_TABLE])
            {
                quotes.Add(Quote.ToQuoteFromTable(pair));
            }

            return quotes;
        }

        #endregion

        #region quotes by authors

        public async Task<List<string>> GetAuthors(int amount = 10)
        {
            int count = await getTableCount(QUOTES_AUTHORS_TABLE);
            amount = Math.Min(count, amount);

            // Because we are fetching the authors without a specific filter
            ScanRequest scanRequest = new ScanRequest
            {
                TableName = QUOTES_AUTHORS_TABLE,
                Limit = amount,
            };

            // Scan
            ScanResponse response = await _client.ScanAsync(scanRequest);

            List<string> authors = new List<string>(response.Count);
            foreach (Dictionary<string, AttributeValue> pair in response.Items)
            {
                authors.Add(pair[QUOTES_AUTHORS_TABLE_HASH_KEY].S);
            }

            return authors;
        }

        public async Task<List<Quote>> GetQuotesByAuthorName(string author, int amount)
        {
            // Get author, if does not exist return null
            GetItemRequest authorRequest = new GetItemRequest
            {
                TableName = QUOTES_AUTHORS_TABLE,
                Key = new Dictionary<string, AttributeValue>
                {
                    {QUOTES_AUTHORS_TABLE_HASH_KEY, new AttributeValue{ S = author } }
                },
                
            };

            GetItemResponse authorResponse = await _client.GetItemAsync(authorRequest);
            if (authorResponse.HttpStatusCode == System.Net.HttpStatusCode.BadRequest ||
                // Does not exist
                !authorResponse.Item.ContainsKey(QUOTES_AUTHORS_TABLE_QUOTES_IDS))
                return null;

            // For data aggregation that is why will not map from string to int for id, will use it as string
            List<string> idsStrings = new List<string>(Math.Min(authorResponse.Item.Count, amount));
            foreach (string id in authorResponse.Item[QUOTES_AUTHORS_TABLE_QUOTES_IDS].NS)
            {
                // To limit how many to fetch
                if (amount < 1)
                    break;

                idsStrings.Add(id);
                amount--;
            }

            return await getQuotesByIds(idsStrings);
        }

        #endregion

        #region quote proposals

        public async Task<bool> SubmitQuote(QuoteWriteDTO quote)
        {
            Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>
            {
                {QUOTES_PROPOSAL_TABLE_HASH_KEY, new AttributeValue{ S = DateTime.UtcNow.ToString() } },
                {"Text", new AttributeValue{ S = quote.Text } },
                {"Quoter", new AttributeValue{ S = quote.Quoter } },
                {"SubmitterEmail", new AttributeValue{ S = quote.SubmitterEmail } },
            };

            PutItemRequest request = new PutItemRequest
            {
                TableName = QUOTES_PROPOSAL_TABLE,
                Item = attributes
            };

            PutItemResponse response = await _client.PutItemAsync(request);
            return response.HttpStatusCode == HttpStatusCode.OK;
        }

        #endregion
    }
}
