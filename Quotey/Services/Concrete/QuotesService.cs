﻿using Amazon;
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

        #region get random

        public async Task<Quote> GetRandomQuote()
        { 
            int randomQuoteId = _random.Next(1, (await getQuotesTableCount()) + 1);
            return await GetQuoteById(randomQuoteId);
        }

        /*public async List<Quote> GetRandomQuotes(int amount = 10)
        {
            int count = await getQuotesTableCount();

            HashSet<int> ids = new HashSet<int>(amount);
            List<Dictionary<string, AttributeValue>> idsValues = new List<Dictionary<string, AttributeValue>>(amount);
            // Generate random Id that is not within list
            // Use amount to represent how much left
            while (amount > 1)
            {
                int randId = _random.Next(1, count + 1);
                
                // Make sure it is not already fetched
                if (!ids.Contains(randId))
                {
                    // It is a long tree since batch item fetch can allow multiple keys
                    ids.Add(randId);
                    idsValues.Add(new Dictionary<string, AttributeValue>
                    {
                        {"Id", new AttributeValue{ N = randId.ToString() }}
                    });

                    amount--;
                }

                BatchGetItemRequest request = new BatchGetItemRequest(new Dictionary<string, KeysAndAttributes>
                { {QUOTES_TABLE, new KeysAndAttributes { Keys = idsValues } } });
                BatchGetItemResponse response = await _client.BatchGetItemAsync(request);

                // Extract data from the complex structure of response
                List<Quote> quotes = new List<Quote>();
                foreach ()
                {

                }
                response.Responses[QUOTES_TABLE].
            }


        }*/

        private async Task<int> getQuotesTableCount()
        {
            DescribeTableResponse description = await _client.DescribeTableAsync(QUOTES_TABLE);
            // The reason for the one, at the start I only inserted one record and
            // since Dynamodb takes 6 hours to update the item count,
            // so it is useful for the first 6 hours of production and that is it.
            int count = description.Table.ItemCount == 0 ? 1 : (int)description.Table.ItemCount;

            return _random.Next(1, count + 1);
        }

        public async Task<Quote> GetQuoteById(int id)
        {
            GetItemRequest itemRequest = new GetItemRequest
            {
                TableName = QUOTES_TABLE,
                Key = new Dictionary<string, AttributeValue>
                {
                    {"Id", new AttributeValue{ N = id.ToString() } }
                }
            };

            GetItemResponse response = await _client.GetItemAsync(itemRequest);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
                // The reason for BadRequest when item does not exist is because of AWS instead 404
                return null;

            return Quote.ToQuoteFromTable(response.Item);
        }

        #endregion
    }
}
