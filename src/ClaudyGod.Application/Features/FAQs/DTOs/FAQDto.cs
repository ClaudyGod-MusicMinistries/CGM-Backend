namespace ClaudyGod.Application.Features.FAQs.DTOs;

public class FAQDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Order { get; set; }
}
