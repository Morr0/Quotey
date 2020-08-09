using AutoMapper;
using Quotey.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quotey.Utilities
{
    public class ModelMappingProfiles : Profile
    {
        public ModelMappingProfiles()
        {
            CreateMap<Quote, QuoteReadDTO>();
        }
    }
}
