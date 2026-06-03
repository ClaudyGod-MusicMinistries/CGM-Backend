namespace ClaudyGod.Domain.Entities;

public class FAQ : AuditableEntity
{
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public int Order { get; private set; } = 0;
    public bool IsPublished { get; private set; } = true;

    protected FAQ() { }

    public static FAQ Create(string question, string answer, string category, int order = 0) =>
        new()
        {
            Question = question.Trim(),
            Answer = answer.Trim(),
            Category = category.Trim(),
            Order = order,
            IsPublished = true
        };

    public void Update(string question, string answer, string category, int order)
    {
        Question = question.Trim();
        Answer = answer.Trim();
        Category = category.Trim();
        Order = order;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
}
