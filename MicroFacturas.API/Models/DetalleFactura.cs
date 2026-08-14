namespace MicroFacturas.API.Models;

public class DetalleFactura
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public int ProductoId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public Factura? Factura { get; set; }
}