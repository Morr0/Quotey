using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.NETCore.Setup;
using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public class QuotesService : IQuotesService
    {
        private AmazonDynamoDBClient _client;
        public QuotesService()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;
            _client = new AmazonDynamoDBClient(region);

            Console.WriteLine("1");

            // Block until setup is done
            Task.WaitAll(setupTablesIfNotSetup());
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
                await _client.DescribeTableAsync("quotey_quote");
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
                    TableName = "quotey_quote",
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

        public async Task<Quote> GetQuoteByQuoter(string quoter)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<string>> GetQuoters()
        {
            throw new System.NotImplementedException();
        }

        public async Task<Quote> GetRandomQuote()
        {
            return new Quote
            {
                Quoter = "Rami"
            };
        }
    }
}
