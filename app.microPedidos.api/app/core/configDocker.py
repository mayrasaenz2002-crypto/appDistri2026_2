DATABASE_URL = (
    "mysql+mysqlconnector://root:admin"
    "@database-mysql:3306/microPedidos"
)

RABBITMQ = {
    "username": "admin",
    "password": "admin",
    "virtualHost": "/",
    "port": 5672,
    "hostname": "component-event-rabbitmq",
    "queue": "clienteDireccionEvent",
    "pedido_queue": "pedidoRegistradoEvent"
}