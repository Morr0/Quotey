using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Services.Mock
{
    public class MockQuotesService : IQuotesService
    {
        public Quote GetRandomQuote()
        {
            return new Quote
            {
                Id = 1,
                Text = "Time lives on...",
                Quoter = "Rami Hikmat",
                SubmitterEmail = "rami@ramihikmat.net"
            };
        }
    }
}
