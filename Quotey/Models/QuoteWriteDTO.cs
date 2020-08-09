using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Quotey.Models
{

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
