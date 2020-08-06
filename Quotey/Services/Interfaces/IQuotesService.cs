using Quotey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Services
{
    public interface IQuotesService
    {
        Task<Quote> GetRandomQuote();
        Task<List<string>> GetQuoters();
        Task<Quote> GetQuoteByQuoter(string quoter);
    }
}
