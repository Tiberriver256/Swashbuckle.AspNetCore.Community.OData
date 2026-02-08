using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ManualValidation.Controllers;

/// <summary>
/// Suppliers OData controller testing singleton and entity set patterns.
/// </summary>
public class SuppliersController : ODataController
{
    private readonly AppDbContext _db;

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all suppliers.
    /// </summary>
    [HttpGet]
    [EnableQuery(PageSize = 50)]
    public IQueryable<Supplier> Get()
    {
        return _db.Suppliers;
    }

    /// <summary>
    /// Get a single supplier by key.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public SingleResult<Supplier> Get([FromRoute] int key)
    {
        return SingleResult.Create(_db.Suppliers.Where(s => s.Id == key));
    }

    /// <summary>
    /// Get the primary supplier (singleton).
    /// Path: ~/PrimarySupplier
    /// </summary>
    [HttpGet("PrimarySupplier")]
    [EnableQuery]
    public SingleResult<Supplier> GetPrimarySupplier()
    {
        return SingleResult.Create(_db.Suppliers.Where(s => s.Id == 1));
    }

    /// <summary>
    /// Get address of a supplier (complex type property).
    /// </summary>
    [HttpGet("Suppliers({key})/Address")]
    public async Task<IActionResult> GetAddress([FromRoute] int key)
    {
        var supplier = await _db.Suppliers.FindAsync(key);
        if (supplier == null)
        {
            return NotFound();
        }

        return Ok(supplier.Address);
    }
}
