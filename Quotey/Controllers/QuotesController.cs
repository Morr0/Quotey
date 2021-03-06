﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Quotey.Controllers.Queries;
using Quotey.Core.Models;
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
            // KEEP IT LAST because it will mess up the pagination otherwise
            if (query.Amount != 1)
            {
                return await GetRandomQuotes(query);
            }

            Quote quote = await _quotesService.GetRandomQuote();
            if (quote == null)
                return NotFound();

            return Ok(_mapper.Map<QuoteReadDTO>(quote));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuoteById([FromRoute] int id)
        {
            Quote quote = await _quotesService.GetQuoteById(id.ToString());
            if (quote == null)
                return NotFound();

            return Ok(_mapper.Map<QuoteReadDTO>(quote));
        }

        // Private for seperation of concers
        private async Task<IActionResult> GetRandomQuotes([FromBody] QuotesQuery query)
        {
            return Ok(_mapper.Map<List<QuoteReadDTO>>
                (await _quotesService.GetRandomQuotes(query.Amount)));
        }

        // Adding new quotes
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] QuoteWriteDTO quote)
        {
            string referenceId = await _quotesService.SubmitQuote(quote);
            if (string.IsNullOrEmpty(referenceId))
                return BadRequest();

            return Ok(new { referenceId });
        }
    }
}
