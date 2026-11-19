using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
using VeterinariaApp.Negocio;
using VeterinariaApp.Negocio.Servicios;
namespace VeterinariaApp.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize]
public class ClientesController(IClienteService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> ObtenerTodos(CancellationToken ct) =>
        Ok(await service.ObtenerTodosAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ClienteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClienteDto>> ObtenerPorId(int id, CancellationToken ct)
    {
        var dto = await service.ObtenerPorIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Recepcionista)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClienteDto>> Crear(ClienteCrearDto dto, CancellationToken ct)
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
    [Authorize(Roles = Roles.Recepcionista)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(int id, ClienteActualizarDto dto, CancellationToken ct)
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
    [Authorize(Roles = Roles.Recepcionista)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    [HttpGet("{clienteId:int}/mascotas")]
    [ProducesResponseType<List<MascotaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<MascotaDto>>> ObtenerMascotas(int clienteId, CancellationToken ct)
    {
        var mascotas = await service.ObtenerMascotasAsync(clienteId, ct);
        return mascotas is null ? NotFound() : Ok(mascotas);
    }
}
