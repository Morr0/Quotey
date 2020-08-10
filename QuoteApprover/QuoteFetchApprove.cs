using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Quotey.Core.Models;
using QuoteyCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuoteApprover
{
    public class QuoteFetchApprove
    {
        private AmazonDynamoDBClient _client;

        private int initialTableCount = 3;
        private int currentTableCount = 3;

        public QuoteFetchApprove()
        {
            RegionEndpoint region = RegionEndpoint.APSoutheast2;
            _client = new AmazonDynamoDBClient(region);

            
        }

        public async Task Run()
        {
            IEnumerable<Quote> quotes = await GetAllQuotesToBeApproved();
            IEnumerable<Quote> approvedQuotes = GetApproved(ref quotes);
            await AddAll(approvedQuotes);

        }

        private async Task<IEnumerable<Quote>> GetAllQuotesToBeApproved()
        {
            ScanRequest scanRequest = new ScanRequest
            {
                TableName = DataDefinitions.QUOTES_PROPOSAL_TABLE,
            };

            ScanResponse scanResponse = await _client.ScanAsync(scanRequest);
            LinkedList<Quote> quotes = new LinkedList<Quote>();
            foreach (var attribute in scanResponse.Items)
            {
                quotes.AddLast(QuoteExtensions.CreateQuoteFromQuoteProposal(attribute));
            }

            // TODO handle over 1MB of scan limit to scan the next items

            return quotes;
        }

        private IEnumerable<Quote> GetApproved(ref IEnumerable<Quote> toBeApproved)
        {
            LinkedList<Quote> approvedQuotes = new LinkedList<Quote>();
            foreach (Quote quote in toBeApproved)
            {
                if (!Approve(quote))
                    continue;

                AssignId(quote);
                approvedQuotes.AddLast(quote);
            }

            return approvedQuotes;
        }

        private async Task AddAll(IEnumerable<Quote> quotes)
        {
            List<Task> tasks = new List<Task>();

            int currentCount = 0;
            LinkedList<Quote> batch = new LinkedList<Quote>();
            foreach (Quote quote in quotes)
            {
                if (currentCount > 25)
                {
                    Task task = WriteBatch(batch);
                    tasks.Add(task);

                    // Reset the batch
                    currentCount = 0;
                    batch = new LinkedList<Quote>();
                }

                batch.AddLast(quote);
                currentCount++;
            }

            await Task.WhenAll(tasks);
        }

        // This processes 25 quotes at a time since the max is 25 as per dynamoDB
        private async Task WriteBatch(IEnumerable<Quote> batchQuotes)
        {
            Dictionary<string, List<WriteRequest>> writeRequests = new Dictionary<string, List<WriteRequest>>();
            writeRequests.Add(DataDefinitions.QUOTES_PROPOSAL_TABLE, new List<WriteRequest>());
            foreach (Quote quote in batchQuotes)
            {
                PutRequest request = new PutRequest(QuoteExtensions.AttributesOfQuote(quote));
                writeRequests[DataDefinitions.QUOTES_PROPOSAL_TABLE].Add(new WriteRequest(request));
            }

            BatchWriteItemRequest batchRequest = new BatchWriteItemRequest
            {
                RequestItems = writeRequests
            };

            BatchWriteItemResponse batchResponse = await _client.BatchWriteItemAsync(batchRequest);
        }

        private void AssignId(Quote quote)
        {
            quote.Id = currentTableCount;
            currentTableCount++;
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
