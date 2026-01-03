using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public CategoriasController(AppDbContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _ctx.Categorias.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .ToListAsync());
}
