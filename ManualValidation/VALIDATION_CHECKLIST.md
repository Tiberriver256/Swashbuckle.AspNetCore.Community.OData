# Manual Validation Checklist

This checklist verifies that the enhanced OData Swagger implementation works correctly.

## 🚀 Running the Validation API

```bash
cd ManualValidation
dotnet run
```

Then open the Swagger URL printed at startup (commonly: **http://localhost:5000/swagger**).

## ✅ Validation Tests

### 1. Swagger UI Loads
- [ ] Swagger UI opens without errors
- [ ] Three API versions are shown:
  - "OData API v1 (Default Route)"
  - "OData API v2 (Advanced)"
  - "Standard REST API"

### 2. OData Query Options Documentation

Navigate to **OData API v1** → **Products** → **GET /Products**

Verify these parameters are present:
- [ ] `$filter` - with example "Name eq 'Product A' and Price gt 100"
- [ ] `$select` - with example "Id,Name,Price,Category"
- [ ] `$expand` - with example "Category($select=Name),Supplier"
- [ ] `$orderby` - with example "Price desc,Name asc"
- [ ] `$top` - with type "integer" and maximum 100
- [ ] `$skip` - with type "integer"
- [ ] `$count` - with type "boolean"
- [ ] `$search` - included in list
- [ ] `$format` - with enum values for JSON formats

### 3. HTTP Methods Coverage

Navigate to **Products** endpoints:

- [ ] `GET /Products` - List with query options
- [ ] `POST /Products` - Create with request body schema
- [ ] `GET /Products({key})` - Get single
- [ ] `PUT /Products({key})` - Full update with request body
- [ ] `PATCH /Products({key})` - Partial update (Delta<T>)
- [ ] `DELETE /Products({key})` - Delete

### 4. Method Overloads

Navigate to **Categories** → Check for:
- [ ] `GET /Categories` - List
- [ ] `GET /Categories({key})` - Single
- [ ] `GET /Categories({key})/Products` - Navigation property
- [ ] `GET /Categories({key})/Name` - Property access
- [ ] `GET /Categories({key})/Name/$value` - Raw value

### 5. OData Functions

Navigate to **Products** → Look for:
- [ ] `GET /Products/GetByPriceRange(minPrice={minPrice},maxPrice={maxPrice})`
  - Verify parameters: minPrice, maxPrice
  - Verify response: Collection of Products

### 6. OData Actions

Navigate to **Products** → Look for:
- [ ] `POST /Products({key})/Rate`
  - Verify parameters: rating, comment
  - Verify response schema

### 7. Singleton

Navigate to **Suppliers** → Look for:
- [ ] `GET /PrimarySupplier` - Singleton endpoint

### 8. Complex Types

Navigate to **Suppliers** → Look for:
- [ ] `GET /Suppliers({key})/Address` - Complex type property

### 9. Navigation Property $ref

Verify these paths are documented (may be under expanded sections):
- [ ] `GET /Products({key})/Category/$ref`
- [ ] `PUT /Products({key})/Category/$ref`
- [ ] `DELETE /Products({key})/Category/$ref`

### 10. Multiple API Versions

Switch between API versions in the top-right dropdown:

**v1 (odata route):**
- [ ] Shows Products, Categories, Suppliers
- [ ] Shows PrimarySupplier singleton

**v2 (v2 route):**
- [ ] Shows Products, Categories, Customers
- [ ] Shows CanPurchase function on Customers
- [ ] Shows GetPremium function on Customers

### 11. Execute Queries (Optional but Recommended)

Try executing some requests:

```
GET /odata/Products?$top=5
GET /odata/Products?$filter=Price gt 50
GET /odata/Products?$select=Id,Name,Price
GET /odata/Products?$expand=Category
GET /odata/Products(1)
GET /odata/Categories(1)/Name
GET /odata/PrimarySupplier
```

### 12. OpenAPI Document Structure

Open raw JSON at **http://localhost:5000/swagger/v1/swagger.json**:

- [ ] `openapi` version is "3.0.1"
- [ ] `info.title` is "OData Validation API (v1)"
- [ ] `paths` contains at least 15+ paths
- [ ] `components.schemas` contains Product, Category, Supplier schemas
- [ ] Product schema includes all properties with correct types
- [ ] Schemas include OData annotations (@odata.count, @odata.nextLink)

## 🐛 Common Issues

### Issue: Query options not showing
**Solution:** Check that `[EnableQuery]` attribute is on controller actions.

### Issue: Missing paths
**Solution:** Verify EDM model is correctly registered in Program.cs.

### Issue: Swagger UI shows error
**Solution:** Check console logs for schema generation errors.

## 📝 Expected vs Actual

Document any discrepancies here:

| Feature | Expected | Actual | Status |
|---------|----------|--------|--------|
| | | | |

## 🎉 Success Criteria

The PR is ready to merge if:
- [ ] All OData query options are documented
- [ ] All CRUD operations show correct HTTP methods
- [ ] Property access, $value, $ref paths are present
- [ ] Functions and actions are documented
- [ ] Multiple API versions work correctly
- [ ] No errors in Swagger UI
