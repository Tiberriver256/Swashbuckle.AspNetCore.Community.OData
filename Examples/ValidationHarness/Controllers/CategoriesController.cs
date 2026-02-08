using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ValidationHarness.Controllers;

/// <summary>
/// Categories OData controller for testing navigation properties and $expand.
/// </summary>
public class CategoriesController : ODataController
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all categories with their products (expandable).
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public IQueryable<Category> Get()
    {
        return _db.Categories;
    }

    /// <summary>
    /// Get a single category by key.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public SingleResult<Category> Get([FromRoute] int key)
    {
        return SingleResult.Create(_db.Categories.Where(c => c.Id == key));
    }

    /// <summary>
    /// Get products for a specific category.
    /// Navigation property path: ~/Categories({key})/Products
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 100)]
    public IQueryable<Product> GetProducts([FromRoute] int key)
    {
        return _db.Products.Where(p => p.CategoryId == key);
    }

    /// <summary>
    /// Get the name property of a category.
    /// Property access path: ~/Categories({key})/Name
    /// </summary>
    [HttpGet("Categories({key})/Name")]
    public async Task<IActionResult> GetName([FromRoute] int key)
    {
        var category = await _db.Categories.FindAsync(key);
        if (category == null)
        {
            return NotFound();
        }

        return Ok(category.Name);
    }

    /// <summary>
    /// Get the raw value of the name property.
    /// Value path: ~/Categories({key})/Name/$value
    /// </summary>
    [HttpGet("Categories({key})/Name/$value")]
    public async Task<IActionResult> GetNameValue([FromRoute] int key)
    {
        var category = await _db.Categories.FindAsync(key);
        if (category == null)
        {
            return NotFound();
        }

        return Content(category.Name, "text/plain");
    }
}
