import { useState } from "react";
import "./App.css";

function App() {
  const [numeroFactura, setNumeroFactura] = useState("");
  const [pedidoId, setPedidoId] = useState("");
  const [clienteId, setClienteId] = useState("");
  const [productoId, setProductoId] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [cantidad, setCantidad] = useState(1);
  const [precioUnitario, setPrecioUnitario] = useState("");

  const subtotal = Number(cantidad) * Number(precioUnitario || 0);
  const iva = subtotal * 0.15;
  const total = subtotal + iva;

  const crearFactura = async () => {
    const factura = {
      numeroFactura,
      pedidoId: Number(pedidoId),
      clienteId: Number(clienteId),
      fecha: new Date().toISOString(),
      estado: "EMITIDA",
      detalles: [
        {
          productoId: Number(productoId),
          descripcion,
          cantidad: Number(cantidad),
          precioUnitario: Number(precioUnitario),
        },
      ],
    };

    try {
      const respuesta = await fetch(
        "http://localhost:5030/api/Facturas",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(factura),
        }
      );

      if (!respuesta.ok) {
        throw new Error("Error al crear la factura");
      }

      const resultado = await respuesta.json();

      alert(
        `Factura creada correctamente\nNúmero: ${resultado.numeroFactura}\nTotal: $${resultado.total}`
      );

      limpiarFormulario();
    } catch (error) {
      console.error(error);
      alert("No se pudo crear la factura");
    }
  };

  const limpiarFormulario = () => {
    setNumeroFactura("");
    setPedidoId("");
    setClienteId("");
    setProductoId("");
    setDescripcion("");
    setCantidad(1);
    setPrecioUnitario("");
  };

  return (
    <div className="app">

      <header>
        <h1>MicroFacturas</h1>
        <p>Sistema de gestión de facturas</p>
      </header>

      <main className="factura-container">

        <h2>Nueva Factura</h2>

        <section className="formulario">

          <div className="campo">
            <label>Número de factura</label>
            <input
              type="text"
              placeholder="FAC-0003"
              value={numeroFactura}
              onChange={(e) => setNumeroFactura(e.target.value)}
            />
          </div>

          <div className="campo">
            <label>Pedido ID</label>
            <input
              type="number"
              value={pedidoId}
              onChange={(e) => setPedidoId(e.target.value)}
            />
          </div>

          <div className="campo">
            <label>Cliente ID</label>
            <input
              type="number"
              value={clienteId}
              onChange={(e) => setClienteId(e.target.value)}
            />
          </div>

        </section>

        <h3>Detalle de factura</h3>

        <section className="formulario">

          <div className="campo">
            <label>Producto ID</label>
            <input
              type="number"
              value={productoId}
              onChange={(e) => setProductoId(e.target.value)}
            />
          </div>

          <div className="campo">
            <label>Descripción</label>
            <input
              type="text"
              placeholder="Rosas rojas"
              value={descripcion}
              onChange={(e) => setDescripcion(e.target.value)}
            />
          </div>

          <div className="campo">
            <label>Cantidad</label>
            <input
              type="number"
              min="1"
              value={cantidad}
              onChange={(e) => setCantidad(e.target.value)}
            />
          </div>

          <div className="campo">
            <label>Precio unitario</label>
            <input
              type="number"
              step="0.01"
              placeholder="20.00"
              value={precioUnitario}
              onChange={(e) => setPrecioUnitario(e.target.value)}
            />
          </div>

        </section>

        <section className="resumen">

          <div>
            <span>Subtotal:</span>
            <strong>${subtotal.toFixed(2)}</strong>
          </div>

          <div>
            <span>IVA 15%:</span>
            <strong>${iva.toFixed(2)}</strong>
          </div>

          <div className="total">
            <span>Total:</span>
            <strong>${total.toFixed(2)}</strong>
          </div>

        </section>

        <button
          className="btn-crear"
          onClick={crearFactura}
        >
          CREAR FACTURA
        </button>

      </main>

    </div>
  );
}

export default App;