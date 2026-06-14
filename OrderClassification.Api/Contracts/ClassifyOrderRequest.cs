using System.ComponentModel.DataAnnotations;
namespace OrderClassification.Api.Contracts;


public sealed record ClassifyOrderRequest
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Classification { get; init; } = string.Empty;
}