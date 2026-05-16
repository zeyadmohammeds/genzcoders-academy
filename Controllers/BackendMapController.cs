using GenZCoders.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Controllers;

[ApiController]
[Route("api/backend-map")]
public class BackendMapController(AcademyDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var entities = db.Model.GetEntityTypes()
            .Select(entity => new
            {
                entity = entity.ClrType.Name,
                table = entity.GetTableName(),
                columns = entity.GetProperties()
                    .Select(property => new
                    {
                        name = property.Name,
                        type = property.ClrType.Name,
                        required = !property.IsNullable,
                        primaryKey = property.IsPrimaryKey()
                    })
                    .OrderBy(x => x.name)
            })
            .OrderBy(x => x.table)
            .ToList();

        return Ok(new { entityCount = entities.Count, entities });
    }
}
