using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VeterinariaApp.Entidades.DTOs;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;
namespace VeterinariaApp.Api.Controllers;

[ApiController]
[Route("api/mascotas")]
public class MascotasController(IMascotaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MascotaDto>>> ObtenerTodos(CancellationToken ct) =>
        Ok(await service.ObtenerTodosAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MascotaDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var dto = await service.ObtenerPorIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
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
