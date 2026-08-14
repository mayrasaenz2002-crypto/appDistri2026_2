using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroFacturas.API.Data;
using MicroFacturas.API.Models;

namespace MicroFacturas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacturasController : ControllerBase
{
    private readonly AppDbContext _context;

    public FacturasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/facturas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Factura>>> GetFacturas()
    {
        var facturas = await _context.Facturas
            .Include(f => f.Detalles)
            .ToListAsync();

        return Ok(facturas);
    }

    // GET: api/facturas/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Factura>> GetFactura(int id)
    {
        var factura = await _context.Facturas
            .Include(f => f.Detalles)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (factura == null)
        {
            return NotFound(new
            {
                mensaje = "Factura no encontrada"
            });
        }

        return Ok(factura);
    }

    // POST: api/facturas
    [HttpPost]
    public async Task<ActionResult<Factura>> CrearFactura(Factura factura)
    {
        if (factura.Detalles == null || factura.Detalles.Count == 0)
        {
            return BadRequest(new
            {
                mensaje = "La factura debe tener al menos un detalle"
            });
        }

        foreach (var detalle in factura.Detalles)
        {
            detalle.Subtotal = detalle.Cantidad * detalle.PrecioUnitario;
        }

        factura.Subtotal = factura.Detalles.Sum(d => d.Subtotal);

        factura.Iva = factura.Subtotal * 0.15m;

        factura.Total = factura.Subtotal + factura.Iva;

        factura.Fecha = factura.Fecha == default
            ? DateTime.UtcNow
            : factura.Fecha;

        factura.Estado = string.IsNullOrWhiteSpace(factura.Estado)
            ? "EMITIDA"
            : factura.Estado;

        _context.Facturas.Add(factura);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetFactura),
            new { id = factura.Id },
            factura
        );
    }

    // DELETE: api/facturas/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarFactura(int id)
    {
        var factura = await _context.Facturas.FindAsync(id);

        if (factura == null)
        {
            return NotFound(new
            {
                mensaje = "Factura no encontrada"
            });
        }

        _context.Facturas.Remove(factura);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}