using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/mesas")]
public class MesasController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public MesasController(AppDbContext ctx) => _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _ctx.Mesas.AsNoTracking()
            .OrderBy(m => m.IdMesa)
            .ToListAsync());
}
