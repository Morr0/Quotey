using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.NETCore.Setup;
using Microsoft.AspNetCore.Http;
using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public class QuotesService : IQuotesService
    {
        // Table 
        public static string QUOTES_TABLE = "quotey_quote";

        private AmazonDynamoDBClient _client;
        private Random _random;

        public QuotesService()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;
            _client = new AmazonDynamoDBClient(region);

            Console.WriteLine("1");

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
            Console.WriteLine("2");
            bool quoteyQuoteTableExists = false;

            // Check for whether the quotey_quote table exists
            try
            {
                await _client.DescribeTableAsync(QUOTES_TABLE);
                quoteyQuoteTableExists = true;
            } catch (ResourceNotFoundException) { }

            Console.WriteLine("3");

            // Create table if does not exist
            if (!quoteyQuoteTableExists)
            {
                Console.WriteLine("quotey_quote does not exist will try to create one");

                // Table attributes
                List<AttributeDefinition> attributes = new List<AttributeDefinition>
                {
                    // Only the indexes
                    new AttributeDefinition("Id", ScalarAttributeType.N),
                };

                // Key Schema
                List<KeySchemaElement> keySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement("Id", KeyType.HASH)
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
        }

        public async Task<Quote> GetRandomQuote()
        {
            DescribeTableResponse description = await _client.DescribeTableAsync(QUOTES_TABLE);

            // The reason for the one, at the start I only inserted one record and
            // since Dynamodb takes 6 hours to update the item count,
            // so it is useful for the first 6 hours of production and that is it.
            int count = description.Table.ItemCount == 0 ? 1 : (int) description.Table.ItemCount;

            int randomQuoteId = _random.Next(1, count + 1);

            // Request quote
            GetItemRequest itemRequest = new GetItemRequest
            {
                TableName = QUOTES_TABLE,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"Id", new AttributeValue{ N = randomQuoteId.ToString() } }
                }
            };

            GetItemResponse item = await _client.GetItemAsync(itemRequest);
            return Quote.ToQuoteFromTable(item.Item);

            // MOCK
            /*return new Quote
            {
                Quoter = "Rami"
            };*/
        }
    }
}
