using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Models
{
    public class QuoteMappingProfile : Profile
    {
        public QuoteMappingProfile()
        {
            CreateMap<Quote, QuoteReadDTO>();
        }
    }

    public class Quote
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Quoter { get; set; }

        public string SubmitterEmail { get; set; }

        public string DateCreated { get; set; }

        public string DateApproved { get; set; }

    }

    public class QuoteReadDTO
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Quoter { get; set; }
    }
}
