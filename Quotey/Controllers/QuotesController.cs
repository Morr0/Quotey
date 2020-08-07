using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quotey.Controllers.Queries;
using Quotey.Models;
using Quotey.Services;

namespace Quotey.Controllers
{
    [Route("api/quotes")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private IMapper _mapper;
        private IQuotesService _quotesService;

        public QuotesController(IMapper mapper, IQuotesService quotesService)
        {
            _mapper = mapper;
            _quotesService = quotesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRandomQuote([FromQuery] QuotesQuery query)
        {
            /*Quote quote = null;
            Console.WriteLine(query.Quoter);
            if (string.IsNullOrEmpty(query.Quoter))
                quote = await _quotesService.GetRandomQuote();
            else
                quote = await _quotesService.GetQuoteByQuoter(query.Quoter);
            */

            Quote quote = await _quotesService.GetRandomQuote();
            return Ok(_mapper.Map<QuoteReadDTO>(quote));
        }
    }
}
