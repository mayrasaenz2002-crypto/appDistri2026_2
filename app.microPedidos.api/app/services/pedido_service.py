from app.services.generic_service import GenericService
from app.core.rabbitmq_producer import RabbitMQProducer
from app.core.config import RABBITMQ


class PedidoService(GenericService):

    def __init__(self, model, database):
        super().__init__(model, database)

    def create(self, data: dict):
        # 1. Guardar en BD
        pedido = super().create(data)

        # 2. Publicar evento
        producer = RabbitMQProducer()

        try:
            message = {
                "id": pedido.id,
                "cliente_direccion_id": pedido.direccion_cliente_id,
                "fecha_pedido": str(pedido.fecha_pedido),
                "total": float(pedido.total)
            }

            producer.publish(RABBITMQ["pedido_queue"], message)

        finally:
            producer.close()

        return pedido