using Amazon.DynamoDBv2.Model;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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

        public static Quote ToQuoteFromTable(Dictionary<string, AttributeValue> item)
        {
            return new Quote
            {
                Id = int.Parse(item["Id"].N),
                Text = item["Text"].S,
                Quoter = item["Quoter"].S,
                SubmitterEmail = item["SubmitterEmail"].S,
                DateCreated = item["SubmitterEmail"].S,
                DateApproved = item["SubmitterEmail"].S
            };
        }

    }

    public class QuoteReadDTO
    {
        public int Id { get; set; }

        public string Text { get; set; }

        public string Quoter { get; set; }
    }

    // Do not map this as it is not currently important since it has to go to approvals first
    public class QuoteWriteDTO
    {
        [Required]
        [NotNull]
        public string Text { get; set; }
        [NotNull]
        public string Quoter { get; set; } = "Wise person";
        [Required]
        [NotNull]
        public string SubmitterEmail { get; set; }
    }
}
