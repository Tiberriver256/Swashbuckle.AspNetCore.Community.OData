using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ValidationHarness.Controllers;

/// <summary>
/// Customers OData controller for v2 API testing.
/// Demonstrates functions, actions, and complex scenarios.
/// </summary>
public class CustomersController : ODataController
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all customers with OData query support.
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 100)]
    public IQueryable<Customer> Get()
    {
        return _db.Customers;
    }

    /// <summary>
    /// Get a single customer by key.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public SingleResult<Customer> Get([FromRoute] int key)
    {
        return SingleResult.Create(_db.Customers.Where(c => c.Id == key));
    }

    /// <summary>
    /// Check if a customer can purchase a product.
    /// OData Function: GET ~/Customers({key})/CanPurchase(productId={productId},maxPrice={maxPrice})
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CanPurchase([FromRoute] int key, [FromODataUri] int productId, [FromODataUri] decimal maxPrice)
    {
        var customer = await _db.Customers.FindAsync(key);
        if (customer == null)
        {
            return NotFound("Customer not found");
        }

        var product = await _db.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound("Product not found");
        }

        // Business logic: Customer can purchase if product price <= maxPrice or customer is premium
        bool canPurchase = product.Price <= maxPrice || customer.IsPremium;

        return Ok(new
        {
            CustomerId = key,
            ProductId = productId,
            ProductPrice = product.Price,
            MaxPrice = maxPrice,
            IsPremium = customer.IsPremium,
            CanPurchase = canPurchase
        });
    }

    /// <summary>
    /// Get premium customers.
    /// OData Function: GET ~/Customers/GetPremium()
    /// Composable: true (can be chained with other queries)
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public IQueryable<Customer> GetPremium()
    {
        return _db.Customers.Where(c => c.IsPremium);
    }
}
