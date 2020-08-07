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
        Task<Quote> GetQuoteById(int id);
        Task<List<Quote>> GetRandomQuotes(int amount);
    }
}
