using ClaudyGod.Domain.Exceptions;

namespace ClaudyGod.Domain.Entities;

public class Product : AuditableEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool InStock { get; private set; } = true;
    public int? Quantity { get; private set; }
    public decimal? Rating { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; } = true;
    public uint Version { get; private set; }

    protected Product() { }

    public static Product Create(
        string title,
        string description,
        decimal price,
        string imageUrl,
        string category,
        bool inStock = true,
        int? quantity = null,
        decimal? rating = null,
        int sortOrder = 0) =>
        new()
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Price = price,
            ImageUrl = imageUrl,
            Category = category,
            InStock = inStock,
            Quantity = quantity,
            Rating = rating,
            SortOrder = sortOrder,
            IsPublished = true
        };

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
    public void UpdateStock(bool inStock, int? quantity) => (InStock, Quantity) = (inStock, quantity);

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Product quantity must be greater than zero.");
        if (!InStock || (Quantity.HasValue && Quantity.Value < quantity))
            throw new DomainException($"'{Title}' does not have enough stock for this order.");

        if (Quantity.HasValue)
        {
            Quantity -= quantity;
            InStock = Quantity > 0;
        }
    }

    public void Update(
        string title,
        string description,
        decimal price,
        string imageUrl,
        string category,
        bool inStock,
        int? quantity,
        decimal? rating,
        int sortOrder)
    {
        Title = title.Trim();
        Description = description.Trim();
        Price = price;
        ImageUrl = imageUrl;
        Category = category;
        InStock = inStock;
        Quantity = quantity;
        Rating = rating;
        SortOrder = sortOrder;
    }
}
