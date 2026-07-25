from pydantic import BaseModel
from datetime import datetime


class PedidoCreate(BaseModel):
    cliente_direccion_id: int
    fecha_pedido: datetime
    total: float


class PedidoResponse(PedidoCreate):
    id: int

    class Config:
        from_attributes = True


class DireccionClienteCreate(BaseModel):
    cliente_id: int
    nombre_completo: str
    email: str
    direccion: str


"""clase para validar las credenciales del token"""
class Usuario(BaseModel):
    email:str
    password:str