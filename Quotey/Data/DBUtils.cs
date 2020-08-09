using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quotey
{
    public class DBUtils
    {
        public static async Task CreateTableIfDoesNotExist(AmazonDynamoDBClient client, string table, string hashKey,
            bool isHashKeyInt = false, string sortKey = null, bool isSortKeyInt = false)
        {
            bool tableExists = false;

            // Check for whether the quotey_quote table exists
            try
            {
                await client.DescribeTableAsync(table);
                tableExists = true;
            }
            catch (ResourceNotFoundException) { }

            // Create table if does not exist
            if (!tableExists)
            {
                Console.WriteLine($"{table} does not exist will try to create one");

                // Table attributes
                List<AttributeDefinition> attributes = new List<AttributeDefinition>
                {
                    // Only the indexes
                    new AttributeDefinition(hashKey, isHashKeyInt? ScalarAttributeType.N: ScalarAttributeType.S)
                };
                // Add sort key if was not null
                if (!string.IsNullOrEmpty(sortKey))
                    attributes.Add(new AttributeDefinition(sortKey, isSortKeyInt ? ScalarAttributeType.N: ScalarAttributeType.S));

                // Key Schema
                List<KeySchemaElement> keySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement(hashKey, KeyType.HASH)
                };
                // Add sort key if was not null
                if (!string.IsNullOrEmpty(sortKey))
                    keySchema.Add(new KeySchemaElement(sortKey, KeyType.RANGE));

                // Table creation request
                CreateTableRequest req = new CreateTableRequest
                {
                    TableName = table,
                    KeySchema = keySchema,
                    AttributeDefinitions = attributes,
                    BillingMode = BillingMode.PAY_PER_REQUEST,
                };

                CreateTableResponse res = await client.CreateTableAsync(req);
                if (res.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception($"Could not create table {table}");

                Console.WriteLine($"{table} successfully created");
            }
        }
    }
}
