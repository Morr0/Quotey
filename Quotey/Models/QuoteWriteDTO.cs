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
        [Required]
        public string Quoter { get; set; }
        [Required]
        [NotNull]
        public string SubmitterEmail { get; set; }
    }
}
