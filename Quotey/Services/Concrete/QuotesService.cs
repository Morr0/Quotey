using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.Http;
using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public class QuotesService : IQuotesService
    {

        #region table definitions

        // Quotes table
        public static string QUOTES_TABLE = "quotey_quote";
        public static string QUOTES_TABLE_HASH_KEY = "Id";

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
            #region quotey_quote
            bool quoteyQuoteTableExists = false;

            // Check for whether the quotey_quote table exists
            try
            {
                await _client.DescribeTableAsync(QUOTES_TABLE);
                quoteyQuoteTableExists = true;
            } catch (ResourceNotFoundException) { }

            // Create table if does not exist
            if (!quoteyQuoteTableExists)
            {
                Console.WriteLine("quotey_quote does not exist will try to create one");

                // Table attributes
                List<AttributeDefinition> attributes = new List<AttributeDefinition>
                {
                    // Only the indexes
                    new AttributeDefinition(QUOTES_TABLE_HASH_KEY, ScalarAttributeType.N),
                };

                // Key Schema
                List<KeySchemaElement> keySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement(QUOTES_TABLE_HASH_KEY, KeyType.HASH)
                };

                // Table creation request
                CreateTableRequest req = new CreateTableRequest
                {
                    TableName = QUOTES_TABLE,
                    KeySchema = keySchema,
                    AttributeDefinitions = attributes,
                    BillingMode = BillingMode.PAY_PER_REQUEST,
                };

                CreateTableResponse res = await _client.CreateTableAsync(req);
                if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("Could not create table quotey_quote");

                Console.WriteLine("quotey_quote successfully created");
            }
            #endregion

            #region quotey_quote_author
            // Motivation: to not use GSI on other tables and worry about provisioning
            // for each new author, store a new record of hash key (Author) and
            // an array of quote primary keys assosciated with it (Quotes).

            bool quoteyQuotesAuthorsTableExists = false;
            try
            {
                await _client.DescribeTableAsync(QUOTES_AUTHORS_TABLE);
                quoteyQuotesAuthorsTableExists = true;
            } catch (ResourceNotFoundException) { }

            if (!quoteyQuotesAuthorsTableExists)
            {
                Console.WriteLine("quotey_quote_author does not exist will try to create one");

                // Table attributes
                List<AttributeDefinition> attributes = new List<AttributeDefinition>
                {
                    // Only the indexes
                    new AttributeDefinition(QUOTES_AUTHORS_TABLE_HASH_KEY, ScalarAttributeType.S),
                };

                // Key Schema
                List<KeySchemaElement> keySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement(QUOTES_AUTHORS_TABLE_HASH_KEY, KeyType.HASH)
                };

                // Table creation request
                CreateTableRequest req = new CreateTableRequest
                {
                    TableName = QUOTES_AUTHORS_TABLE,
                    KeySchema = keySchema,
                    AttributeDefinitions = attributes,
                    BillingMode = BillingMode.PAY_PER_REQUEST,
                };

                CreateTableResponse res = await _client.CreateTableAsync(req);
                if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("Could not create table quotey_quote_author");

                Console.WriteLine("quotey_quote_author successfully created");
            }
            
            #endregion
        }

        #region quotes

        public async Task<Quote> GetRandomQuote()
        { 
            int randomQuoteId = _random.Next(1, (await getTableCount(QUOTES_TABLE)) + 1);
            return await GetQuoteById(randomQuoteId);
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

        public async Task<Quote> GetQuoteById(int id)
        {
            GetItemRequest itemRequest = new GetItemRequest
            {
                TableName = QUOTES_TABLE,
                Key = new Dictionary<string, AttributeValue>
                {
                    {QUOTES_TABLE_HASH_KEY, new AttributeValue{ N = id.ToString() } }
                }
            };

            GetItemResponse response = await _client.GetItemAsync(itemRequest);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.BadRequest ||
                // Id does not exist
                !response.Item.ContainsKey(QUOTES_TABLE_HASH_KEY))
                // The reason for BadRequest when item does not exist is because of AWS instead 404
                return null;

            return Quote.ToQuoteFromTable(response.Item);
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
                }
            };

            GetItemResponse authorResponse = await _client.GetItemAsync(authorRequest);
            if (authorResponse.HttpStatusCode == System.Net.HttpStatusCode.BadRequest ||
                // Does not exist
                !authorResponse.Item.ContainsKey(QUOTES_AUTHORS_TABLE_QUOTES_IDS))
                return null;

            // For data aggregation that is why will not map from string to int for id, will use it as string
            List<string> idsStrings = authorResponse.Item[QUOTES_AUTHORS_TABLE_QUOTES_IDS].NS;
            return await getQuotesByIds(idsStrings);
        }

        #endregion
    }
}
