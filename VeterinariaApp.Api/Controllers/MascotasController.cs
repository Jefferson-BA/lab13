using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;
namespace VeterinariaApp.Api.Controllers;

[ApiController]
[Route("api/mascotas")]
[Authorize]
public class MascotasController(IMascotaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MascotaDto>>> ObtenerTodos(CancellationToken ct) =>
        Ok(await service.ObtenerTodosAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<MascotaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MascotaDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var dto = await service.ObtenerPorIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Veterinario)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MascotaDto>> Crear(MascotaCrearDto dto, CancellationToken ct)
    {
        try
        {
            var creado = await service.CrearAsync(dto, ct);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
        }
        catch (ReglaNegocioException ex)
        {
            return Problem(statusCode: 409, title: "Regla de negocio", detail: ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Veterinario)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, MascotaActualizarDto dto, CancellationToken ct)
    {
        try
        {
            return await service.ActualizarAsync(id, dto, ct) ? NoContent() : NotFound();
        }
        catch (ReglaNegocioException ex)
        {
            return Problem(statusCode: 409, title: "Regla de negocio", detail: ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Veterinario)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        try
        {
            return await service.EliminarAsync(id, ct) ? NoContent() : NotFound();
        }
        catch (ReglaNegocioException ex)
        {
            return Problem(statusCode: 409, title: "Regla de negocio", detail: ex.Message);
        }
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<MascotaDto>>> Buscar(
        [FromQuery, Required, StringLength(50)] string especie, CancellationToken ct) =>
        Ok(await service.BuscarAsync(especie, ct));
}
