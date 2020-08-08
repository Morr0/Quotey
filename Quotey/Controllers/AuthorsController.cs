using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Quotey.Controllers.Queries;
using Quotey.Services;

namespace Quotey.Controllers
{
    [Route("api/authors")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private IQuotesService _quotesService;

        public AuthorsController(IQuotesService quotesService)
        {
            _quotesService = quotesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuthors([FromQuery] AuthorsQuery query)
        {
            return Ok(await _quotesService.GetAuthors(query.Amount));
        }
    }
}
