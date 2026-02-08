using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ValidationHarness.Controllers;

/// <summary>
/// Products OData controller demonstrating all CRUD operations,
/// query options, method overloads, and OData-specific features.
/// </summary>
public class ProductsController : ODataController
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // ==========================================
    // QUERY OPERATIONS
    // ==========================================

    /// <summary>
    /// Get all products with full OData query support.
    /// Supports: $filter, $select, $expand, $orderby, $top, $skip, $count
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 100, AllowedQueryOptions =
        AllowedQueryOptions.Filter |
        AllowedQueryOptions.Select |
        AllowedQueryOptions.Expand |
        AllowedQueryOptions.OrderBy |
        AllowedQueryOptions.Top |
        AllowedQueryOptions.Skip |
        AllowedQueryOptions.Count)]
    public IQueryable<Product> Get()
    {
        return _db.Products;
    }

    /// <summary>
    /// Get a single product by key.
    /// </summary>
    [HttpGet]
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Select | AllowedQueryOptions.Expand)]
    public SingleResult<Product> Get([FromRoute] int key)
    {
        return SingleResult.Create(_db.Products.Where(p => p.Id == key));
    }

    // ==========================================
    // CRUD OPERATIONS
    // ==========================================

    /// <summary>
    /// Create a new product.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return Created(product);
    }

    /// <summary>
    /// Update a product (full replacement).
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Put([FromRoute] int key, [FromBody] Product product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (key != product.Id)
        {
            return BadRequest("Key mismatch");
        }

        var existing = await _db.Products.FindAsync(key);
        if (existing == null)
        {
            return NotFound();
        }

        _db.Entry(existing).CurrentValues.SetValues(product);
        await _db.SaveChangesAsync();

        return Updated(product);
    }

    /// <summary>
    /// Partially update a product using Delta&lt;T&gt;.
    /// This is an OData-specific feature for patch operations.
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> Patch([FromRoute] int key, [FromBody] Delta<Product> delta)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _db.Products.FindAsync(key);
        if (existing == null)
        {
            return NotFound();
        }

        delta.Patch(existing);
        await _db.SaveChangesAsync();

        return Updated(existing);
    }

    /// <summary>
    /// Delete a product.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int key)
    {
        var product = await _db.Products.FindAsync(key);
        if (product == null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ==========================================
    // FUNCTIONS (OData Functions)
    // ==========================================

    /// <summary>
    /// Get products within a price range.
    /// OData Function: GET ~/Products/GetByPriceRange(minPrice={minPrice},maxPrice={maxPrice})
    /// </summary>
    [HttpGet]
    public IQueryable<Product> GetByPriceRange([FromODataUri] decimal minPrice, [FromODataUri] decimal maxPrice)
    {
        return _db.Products.Where(p => p.Price >= minPrice && p.Price <= maxPrice);
    }

    // ==========================================
    // ACTIONS (OData Actions)
    // ==========================================

    /// <summary>
    /// Rate a product.
    /// OData Action: POST ~/Products({key})/Rate
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Rate([FromRoute] int key, ODataActionParameters parameters)
    {
        if (!parameters.TryGetValue("rating", out var ratingObj) ||
            !parameters.TryGetValue("comment", out var comment))
        {
            return BadRequest("Missing rating or comment");
        }

        var rating = (int)ratingObj;

        var product = await _db.Products.FindAsync(key);
        if (product == null)
        {
            return NotFound();
        }

        // In real scenario, save rating to database
        // For demo, just return the rating
        return Ok(new { ProductId = key, Rating = rating, Comment = comment, AverageRating = (double)rating });
    }
}
