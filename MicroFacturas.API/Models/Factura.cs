namespace MicroFacturas.API.Models;

public class Factura
{
    public int Id { get; set; }

    public string NumeroFactura { get; set; } = string.Empty;

    public int PedidoId { get; set; }

    public int ClienteId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = "EMITIDA";

    public ICollection<DetalleFactura> Detalles { get; set; }
        = new List<DetalleFactura>();
}