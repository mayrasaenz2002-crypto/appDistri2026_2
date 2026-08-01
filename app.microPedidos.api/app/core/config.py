import os

if os.getenv("APP_ENV", "local").lower() == "docker":
    from app.core.configDocker import DATABASE_URL, RABBITMQ
else:
    DATABASE_URL = (
        "mysql+mysqlconnector://root:admin"
        "@localhost:3307/microPedidos"
    )

    RABBITMQ = {
        "username": "admin",
        "password": "admin",
        "virtualHost": "/",
        "port": 5672,
        "hostname": "localhost",
        "queue": "clienteDireccionEvent",
        "pedido_queue": "pedidoRegistradoEvent"
    }