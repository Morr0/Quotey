using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Controllers.Queries
{
    public class QuoteQuery
    {
        [NotNull]
        public int? Id { get; set; }
    }
}
