from sqlalchemy import Column, Integer, String, DateTime, Numeric, ForeignKey
from app.core.database import Base


class ClienteDireccion(Base):
    __tablename__ = "cliente_direcciones"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    cliente_id = Column(Integer)
    nombre_completo = Column(String(255))
    email = Column(String(255))
    direccion = Column(String(500))


class Pedido(Base):
    __tablename__ = "pedidos"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    cliente_direccion_id = Column(Integer, ForeignKey("cliente_direcciones.id"))
    fecha_pedido = Column(DateTime)
    total = Column(Numeric(10, 2))


