using Quotey.Core.Models;
using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Services.Mock
{
    public class MockQuotesService// : IQuotesService
    {
        private readonly Dictionary<string, List<Quote>> _quotesByQuoters = new Dictionary<string, List<Quote>>();
        public MockQuotesService()
        {
            // Mock data 1
            string quoter = "Rami Hikmat";
            _quotesByQuoters.Add(quoter, new List<Quote>
            {
                new Quote
                {
                    Id = 1,
                    Text = "Time lives on...",
                    Quoter = quoter,
                    SubmitterEmail = "rami@ramihikmat.net"
                },
                new Quote
                {
                    Id = 2,
                    Text = "No need to stop enjoying what is right...",
                    Quoter = quoter,
                    SubmitterEmail = "rami@ramihikmat.net"
                }
            });

            // Mock data 2
            quoter = "Someone";
            _quotesByQuoters.Add(quoter, new List<Quote>
            {
                new Quote
                {
                    Id = 3,
                    Text = "Brrrr......",
                    Quoter = quoter,
                    SubmitterEmail = "someone@ramihikmat.net"
                },
                new Quote
                {
                    Id = 4,
                    Text = "Sleep is mandatory",
                    Quoter = quoter,
                    SubmitterEmail = "someone@ramihikmat.net"
                }
            });
        }

        public async Task<Quote> GetRandomQuote()
        {
            return new Quote
            {
                Id = 1,
                Text = "Time lives on...",
                Quoter = "Rami Hikmat",
                SubmitterEmail = "rami@ramihikmat.net"
            };
        }
        public async Task<List<string>> GetQuoters()
        {
            return _quotesByQuoters.Keys.ToList();
        }

        public async Task<Quote> GetQuoteByQuoter(string quoter)
        {
            return _quotesByQuoters["Rami Hikmat"][1];
        }
    }
}
