using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Quotey.Core.Models;
using QuoteyCore.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuoteApprover
{
    public class QuoteFetchApprove
    {
        private AmazonDynamoDBClient _client;

        private int initialQuotesTableCount = 3;
        private int currentQuotesTableCount = 3;

        public QuoteFetchApprove()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;
            _client = new AmazonDynamoDBClient(region);
        }

        public async Task Run()
        {
            // Get initial tables counts
            Console.WriteLine("Getting quotes table description");
            initialQuotesTableCount = (int) (await _client.DescribeTableAsync(DataDefinitions.QUOTES_PROPOSAL_TABLE)).Table.ItemCount;
            currentQuotesTableCount = initialQuotesTableCount + 1;
            Console.WriteLine($"The quotes table has {currentQuotesTableCount} records");

            // Start the pipeline
            Console.WriteLine("1");
            IEnumerable<Quote> quotes = await GetAllQuotesToBeApproved();
            Console.WriteLine("2");
            IEnumerable<Quote> approvedQuotes = GetApproved(ref quotes, out var quotersQuotes);
            Console.WriteLine("3");
            await AddAll(approvedQuotes, quotersQuotes);
            Console.WriteLine("Finished");

        }

        private async Task<IEnumerable<Quote>> GetAllQuotesToBeApproved()
        {
            ScanRequest scanRequest = new ScanRequest
            {
                TableName = DataDefinitions.QUOTES_PROPOSAL_TABLE,
            };

            Console.WriteLine("Scanning proposals");
            ScanResponse scanResponse = await _client.ScanAsync(scanRequest);
            LinkedList<Quote> quotes = new LinkedList<Quote>();
            foreach (var attribute in scanResponse.Items)
            {
                quotes.AddLast(QuoteExtensions.CreateQuoteFromQuoteProposal(attribute));
            }
            Console.WriteLine("Got proposals");

            // TODO handle over 1MB of scan limit to scan the next items

            return quotes;
        }

        private IEnumerable<Quote> GetApproved(ref IEnumerable<Quote> toBeApproved, 
            out Dictionary<string, List<string>>  quotersQuotes)
        {
            quotersQuotes = new Dictionary<string, List<string>>();

            LinkedList<Quote> approvedQuotes = new LinkedList<Quote>();
            foreach (Quote quote in toBeApproved)
            {
                Console.WriteLine("Approving");
                if (!Approve(quote))
                    continue;
                Console.WriteLine("This was approved");

                AssignId(quote);
                approvedQuotes.AddLast(quote);
            }

            return approvedQuotes;
        }

        private async Task AddAll(IEnumerable<Quote> quotes, Dictionary<string, List<string>> quotersQuotes)
        {
            List<Task> tasks = new List<Task>();

            int currentCount = 0;
            LinkedList<Quote> batch = new LinkedList<Quote>();
            foreach (Quote quote in quotes)
            {
                /*if (currentCount > 25 || currentCount >= quotes.Count())
                {
                    Task task = WriteBatch(batch);
                    tasks.Add(task);

                    // Reset the batch
                    currentCount = 0;
                    batch = new LinkedList<Quote>();
                }*/

                batch.AddLast(quote);
                currentCount++;
            }
            // TODO temporary
            tasks.Add(WriteBatch(batch));

            await Task.WhenAll(tasks);
        }

        // This processes 25 quotes at a time since the max is 25 as per dynamoDB
        private async Task WriteBatch(IEnumerable<Quote> batchQuotes)
        {
            Console.WriteLine("Writing batch");
            Dictionary<string, List<WriteRequest>> writeRequests = new Dictionary<string, List<WriteRequest>>();
            writeRequests.Add(DataDefinitions.QUOTES_TABLE, new List<WriteRequest>());
            foreach (Quote quote in batchQuotes)
            {
                PutRequest request = new PutRequest(QuoteExtensions.AttributesOfQuote(quote));
                writeRequests[DataDefinitions.QUOTES_TABLE].Add(new WriteRequest(request));
            }

            BatchWriteItemRequest batchRequest = new BatchWriteItemRequest
            {
                RequestItems = writeRequests
            };

            BatchWriteItemResponse batchResponse = await _client.BatchWriteItemAsync(batchRequest);
            Console.WriteLine($"quotes batch response: {batchResponse.HttpStatusCode}");
        }

        private void AssignId(Quote quote)
        {
            quote.Id = currentQuotesTableCount;
            currentQuotesTableCount++;
        }

        // TODO implement this to disallow bad wording
        // Will approve/disapprove a quote, if approved will add an approval date UTC
        private bool Approve(Quote quote)
        {
            quote.DateApproved = DateTime.UtcNow.ToString();
            return true;
        }
    }
}
