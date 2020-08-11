using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Quotey.Core.Models;
using QuoteyCore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace QuoteApprover
{
    public class QuoteFetchApprove
    {
        private AmazonDynamoDBClient _client;

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
            currentQuotesTableCount = ((int)(await _client.DescribeTableAsync(DataDefinitions.QUOTES_PROPOSAL_TABLE))
                .Table.ItemCount) + 1;
            Console.WriteLine($"The quotes table has {currentQuotesTableCount} records");

            // Start the pipeline
            Console.WriteLine("Starting pipeline");
            Console.WriteLine("Step 1");
            IEnumerable<Quote> quotes = await GetAllQuotesToBeApproved();
            Console.WriteLine("Step 2");
            IEnumerable<Quote> approvedQuotes = GetApprovedProposals(ref quotes);
            Console.WriteLine("Step 3");
            await AddAll(approvedQuotes);
            Console.WriteLine("Finished pipeline");

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

        private IEnumerable<Quote> GetApprovedProposals(ref IEnumerable<Quote> toBeApproved)
        {
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
            Console.WriteLine($"Total approved quotes {approvedQuotes.Count}");

            return approvedQuotes;
        }

        private async Task AddAll(IEnumerable<Quote> quotes)
        {
            LinkedList<Quote> batchQuotes = new LinkedList<Quote>();
            int count = quotes.Count();
            int currentCount = 0;
            foreach (Quote quote in quotes)
            {
                batchQuotes.AddLast(quote);

                // Every 25 quotes starting from 25
                if (currentCount > 0 && currentCount % 25 == 0)
                {
                    await WriteBatch(batchQuotes);
                    batchQuotes = new LinkedList<Quote>();
                } 
                // Only for the case when total quotes less than 25
                else if (currentCount == (count - 1))
                {
                    await WriteBatch(batchQuotes);
                }

                currentCount++;
            }
        }

        // This processes 25 quotes at a time since the max is 25 as per dynamoDB batch write
        private async Task WriteBatch(IEnumerable<Quote> batchQuotes)
        {
            // There is this possibility when there is nothing to approve
            if (batchQuotes.Count() == 0)
            {
                Console.WriteLine("Batch empty");
                return;
            }

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

            Console.WriteLine("Before writing to dynamodb");
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
