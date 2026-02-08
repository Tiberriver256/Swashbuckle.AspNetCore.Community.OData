using System.ComponentModel.DataAnnotations;

namespace ValidationHarness;

/// <summary>
/// Product entity for OData validation testing.
/// Demonstrates entity with query restrictions and navigation properties.
/// </summary>
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Range(0, 10000)]
    public int Stock { get; set; }

    public bool IsAvailable { get; set; }

    /// <summary>
    /// This property is marked as non-filterable in EDM configuration.
    /// </summary>
    public string? SecretCode { get; set; }

    // Navigation properties
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public int SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;
}

/// <summary>
/// Category entity with collection navigation property.
/// </summary>
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Supplier entity with complex type property.
/// </summary>
public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public Address Address { get; set; } = new();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Complex type for supplier address.
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

/// <summary>
/// Customer entity for v2 API testing.
/// </summary>
public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool IsPremium { get; set; }

    public decimal TotalSpent { get; set; }
}
