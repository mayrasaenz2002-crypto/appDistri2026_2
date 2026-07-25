# app.microPedidos.api

Microservicio de pedidos construido con **FastAPI**, **SQLAlchemy**, **MySQL** y **RabbitMQ**. La aplicación tiene dos puntos de entrada independientes:

- Una **API HTTP**, responsable del CRUD de pedidos y de publicar el evento `pedidoRegistradoEvent`.
- Un **worker**, responsable de consumir `clienteDireccionEvent`, enviado por `app.microCliente.api`, y almacenar localmente la dirección del cliente.

Ambos procesos comparten los modelos, servicios, configuración y acceso a la misma base de datos, pero deben ejecutarse como procesos separados.

## Arquitectura

```mermaid
flowchart LR
    Client["Cliente HTTP"] -->|"CRUD /pedidos"| API["FastAPI<br/>main_api.py"]
    API --> PS["PedidoService"]
    PS -->|"Guardar pedido"| DB[("MySQL<br/>EcPedidos")]
    PS -->|"Publicar"| PQ["RabbitMQ<br/>pedidoRegistradoEvent"]

    MC["app.microCliente.api"] -->|"Publicar"| CQ["RabbitMQ<br/>clienteDireccionEvent"]
    CQ -->|"Consumir"| WK["Worker<br/>main_worker.py"]
    WK --> GS["GenericService"]
    GS -->|"Guardar dirección"| DB

    Other["Otros consumidores"] -->|"Consumir pedidos registrados"| PQ
```

### Responsabilidades por capa

```text
app.microPedidos.api/
├── main_api.py                    # Punto de entrada de la API FastAPI
├── main_worker.py                 # Punto de entrada del consumidor RabbitMQ
├── requirements.txt               # Dependencias de Python
└── app/
    ├── api/
    │   └── routes.py              # Endpoints HTTP de pedidos
    ├── core/
    │   ├── config.py              # Configuración de MySQL y RabbitMQ
    │   ├── database.py            # Engine, sesiones y Base de SQLAlchemy
    │   └── rabbitmq_producer.py   # Publicador de eventos RabbitMQ
    ├── models/
    │   └── models.py              # Entidades Pedido y ClienteDireccion
    ├── schemas/
    │   └── schemas.py             # Contratos Pydantic de la API
    ├── services/
    │   ├── generic_service.py     # Operaciones CRUD reutilizables
    │   ├── pedido_service.py      # Creación de pedido y publicación del evento
    │   ├── authService.py         # Validación Bearer/JWT
    │   └── jwt_manager.py         # Creación y lectura del token JWT
    └── worker/
        └── consumer.py            # Consumidor de clienteDireccionEvent
```

## Requisitos previos

- Python 3.10 o superior.
- MySQL disponible en `localhost:3307`.
- RabbitMQ disponible en `localhost:5672`.
- Una base de datos MySQL llamada `EcPedidos`.
- El microservicio `app.microCliente.api` para generar el evento de dirección de cliente.

La configuración actual se encuentra en `app/core/config.py`:

```python
DATABASE_URL = "mysql+mysqlconnector://root:admin@localhost:3307/EcPedidos"

RABBITMQ = {
    "username": "admin",
    "password": "admin",
    "virtualHost": "/",
    "port": 5672,
    "hostname": "localhost",
    "queue": "clienteDireccionEvent",
    "pedido_queue": "pedidoRegistradoEvent"
}
```

Adapte estos valores a su ambiente antes de iniciar los procesos. En producción, las credenciales y secretos deben obtenerse desde variables de entorno o un gestor de secretos.

## 1. Crear la base de datos

Ingrese a MySQL y ejecute:

```sql
CREATE DATABASE EcPedidos;
```

El proyecto usa SQLAlchemy Code First y ejecuta `Base.metadata.create_all(...)` al iniciar la API o el worker.

> **Importante:** antes de arrancar, compruebe que la clave foránea de `Pedido` en `app/models/models.py` apunte al nombre real de la tabla de direcciones. La entidad declara la tabla `cliente_direcciones`; por tanto, la referencia coherente es `cliente_direcciones.id`. Si conserva la referencia actual a `direcciones_clientes.id`, SQLAlchemy generará `NoReferencedTableError` durante el inicio.

## 2. Crear el ambiente virtual `myenv`

Desde la raíz de `app.microPedidos.api`, cree el ambiente virtual:

### Windows PowerShell

```powershell
python -m venv myenv
```

Actívelo:

```powershell
.\myenv\Scripts\Activate.ps1
```

Si PowerShell bloquea la activación de scripts, puede habilitarla únicamente para la sesión actual:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\myenv\Scripts\Activate.ps1
```

### Linux o macOS

```bash
python3 -m venv myenv
source myenv/bin/activate
```

Cuando el ambiente esté activo, la consola mostrará `(myenv)` al inicio de la línea.

Para salir posteriormente del ambiente:

```text
deactivate
```

## 3. Instalar las dependencias

Con `myenv` activo, actualice `pip` e instale el proyecto:

```powershell
python -m pip install --upgrade pip
pip install -r requirements.txt
```

Las dependencias principales son:

- `fastapi`: API HTTP.
- `uvicorn`: servidor ASGI.
- `sqlalchemy`: ORM y persistencia.
- `mysql-connector-python`: driver de MySQL.
- `pydantic`: validación de contratos.
- `pika`: comunicación con RabbitMQ.
- `PyJWT`: generación y validación de tokens.

## 4. Levantar RabbitMQ

Si dispone de Docker, puede iniciar RabbitMQ con la consola de administración mediante:

```powershell
docker run -d --name ec-rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin rabbitmq:3-management
```

Servicios expuestos:

- AMQP: `localhost:5672`.
- Consola web: [http://localhost:15672](http://localhost:15672).
- Usuario: `admin`.
- Contraseña: `admin`.

RabbitMQ creará las colas cuando el productor o el consumidor las declare.

## 5. Ejecutar la API

Abra una terminal, ubíquese en la raíz del proyecto y active `myenv`. Después ejecute:

```powershell
uvicorn main_api:app --reload --host 0.0.0.0 --port 8000
```

La API estará disponible en:

- API: [http://localhost:8000](http://localhost:8000).
- Swagger UI: [http://localhost:8000/docs](http://localhost:8000/docs).
- OpenAPI: [http://localhost:8000/openapi.json](http://localhost:8000/openapi.json).

### Obtener un token

Solicitud:

```http
POST /login
Content-Type: application/json
```

```json
{
  "email": "admin@gmail.com",
  "password": "admin"
}
```

Para los endpoints protegidos, envíe el token como Bearer:

```http
Authorization: Bearer <token>
```

### Endpoints de pedidos

| Método | Ruta | Descripción | JWT en el código actual |
|---|---|---|---|
| `GET` | `/pedidos` | Lista todos los pedidos | Sí |
| `GET` | `/pedidos/{id}` | Obtiene un pedido | No |
| `POST` | `/pedidos` | Registra y publica un pedido | Sí |
| `PUT` | `/pedidos/{id}` | Actualiza un pedido | No |
| `DELETE` | `/pedidos/{id}` | Elimina un pedido | No |

Ejemplo para registrar un pedido:

```http
POST /pedidos
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "direccion_cliente_id": 1,
  "fecha_pedido": "2026-07-18T10:30:00",
  "total": 150.75
}
```

La API guarda el pedido y publica un mensaje en `pedidoRegistradoEvent`:

```json
{
  "id": 1,
  "direccion_cliente_id": 1,
  "fecha_pedido": "2026-07-18 10:30:00",
  "total": 150.75
}
```

## 6. Ejecutar el worker

El worker debe ejecutarse simultáneamente con la API, pero en otra terminal.

Abra una segunda terminal, active el mismo ambiente y ejecute:

```powershell
.\myenv\Scripts\Activate.ps1
python main_worker.py
```

El proceso quedará escuchando continuamente la cola `clienteDireccionEvent`:

```text
Iniciando suscriptor RabbitMQ...
Escuchando RabbitMQ...
```

Cuando recibe un mensaje válido, transforma el contrato y crea un registro en `cliente_direcciones`.

## Integración con `app.microCliente.api`

`app.microCliente.api` es el productor del evento `clienteDireccionEvent`. Cuando ese microservicio registra o comunica una dirección, publica un mensaje en RabbitMQ. El worker de este proyecto lo consume y mantiene una copia local de los datos necesarios para asociar pedidos con direcciones.

### Contrato consumido

De acuerdo con la implementación actual de `app/worker/consumer.py`, el mensaje debe contener exactamente:

```json
{
  "ClienteId": 10,
  "NombreCompleto": "Juan Pérez",
  "Email": "juan@example.com",
  "DireccionCompleta": "Av. Amazonas y Naciones Unidas, Quito"
}
```

El worker lo transforma a:

```json
{
  "cliente_id": 10,
  "nombre_completo": "Juan Pérez",
  "email": "juan@example.com",
  "direccion": "Av. Amazonas y Naciones Unidas, Quito"
}
```

> El campo esperado por el código es `DireccionCompleta`, no `Direccion`. El productor `app.microCliente.api` y este consumidor deben compartir el mismo contrato.

## Flujo completo del sistema

### A. Sincronización de la dirección

1. Un cliente registra o actualiza su dirección mediante `app.microCliente.api`.
2. `app.microCliente.api` publica `clienteDireccionEvent` en RabbitMQ.
3. El worker iniciado con `main_worker.py` recibe el mensaje.
4. El worker traduce los campos externos al modelo `ClienteDireccion`.
5. `GenericService` abre una sesión SQLAlchemy.
6. La dirección se inserta en `cliente_direcciones` y se confirma la transacción.
7. El worker continúa esperando nuevos mensajes.

### B. Registro de un pedido

1. El consumidor HTTP solicita un JWT mediante `POST /login`.
2. Envía `POST /pedidos` con el token y los datos del pedido.
3. FastAPI valida el cuerpo con `PedidoCreate`.
4. `PedidoService` utiliza `GenericService` para guardar el pedido en MySQL.
5. Después del commit, `PedidoService` abre una conexión con RabbitMQ.
6. Publica `pedidoRegistradoEvent` con la información del pedido.
7. Cierra la conexión RabbitMQ y devuelve el pedido creado.
8. Cualquier microservicio suscrito a `pedidoRegistradoEvent` puede continuar el proceso distribuido.

## Orden recomendado para levantar todo el entorno

1. Iniciar MySQL en el puerto `3307`.
2. Crear la base de datos `MicroPedidos`.
3. Iniciar RabbitMQ en los puertos `5672` y `15672`.
4. Crear y activar `myenv`.
5. Instalar `requirements.txt`.
6. Iniciar el worker con `python main_worker.py`.
7. Iniciar la API con `uvicorn main_api:app --reload --port 8000`.
8. Iniciar `app.microCliente.api` con su propia configuración.
9. Crear una dirección desde `app.microCliente.api`.
10. Confirmar que el worker insertó la dirección.
11. Obtener un JWT y crear el pedido mediante `POST /pedidos`.
12. Confirmar en RabbitMQ la publicación de `pedidoRegistradoEvent`.

## Prueba manual de RabbitMQ

Si `app.microCliente.api` todavía no está ejecutándose, puede probar el worker desde la consola web de RabbitMQ:

1. Abra [http://localhost:15672](http://localhost:15672).
2. Ingrese con `admin` / `admin`.
3. Abra **Queues and Streams**.
4. Seleccione `clienteDireccionEvent`.
5. En **Publish message**, use el contrato JSON mostrado anteriormente.
6. Publique el mensaje y revise la terminal del worker.

## Persistencia

El microservicio maneja dos tablas:

### `cliente_direcciones`

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | Integer | Clave primaria local |
| `cliente_id` | Integer | Identificador proveniente de `app.microCliente.api` |
| `nombre_completo` | String(255) | Nombre del cliente |
| `email` | String(255) | Correo electrónico |
| `direccion` | String(500) | Dirección completa |

### `pedidos`

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | Integer | Clave primaria |
| `direccion_cliente_id` | Integer | Referencia a la dirección local |
| `fecha_pedido` | DateTime | Fecha y hora del pedido |
| `total` | Numeric(10,2) | Valor total del pedido |

## Consideraciones de confiabilidad

La implementación actual es apropiada como ejemplo académico, pero antes de usarla en producción se recomienda:

- Desactivar `auto_ack=True` y confirmar el mensaje sólo después del commit.
- Configurar reintentos y una dead-letter queue para eventos inválidos.
- Añadir un `event_id` e idempotencia para evitar registros duplicados.
- Declarar colas y mensajes como durables/persistentes.
- Implementar publisher confirms.
- Usar transactional outbox para mantener consistencia entre MySQL y RabbitMQ.
- Reutilizar conexiones RabbitMQ en lugar de abrir una por pedido.
- Sustituir `create_all()` por migraciones con Alembic.
- Mover credenciales y el secreto JWT a variables de entorno.
- Añadir expiración al JWT y proteger uniformemente el CRUD.
- Añadir logging estructurado, health checks y pruebas automatizadas.

## Solución de problemas

### `NoReferencedTableError`

Revise que la clave foránea de `Pedido` apunte a `cliente_direcciones.id`, que es el nombre declarado por `ClienteDireccion`.

### No se puede conectar a MySQL

Compruebe:

- Que MySQL esté iniciado.
- Que esté escuchando en el puerto `3307`.
- Que exista la base `MicroPedidos`.
- Que usuario y contraseña coincidan con `DATABASE_URL`.

### No se puede conectar a RabbitMQ

Compruebe:

- Que el contenedor o servicio esté iniciado.
- Que el puerto `5672` esté disponible.
- Que existan el usuario `admin` y la contraseña `admin`.
- Que ambos microservicios utilicen el mismo host, virtual host y nombre de cola.

### El worker recibe el mensaje, pero no guarda datos

Verifique que el JSON incluya `ClienteId`, `NombreCompleto`, `Email` y `DireccionCompleta`, respetando mayúsculas y minúsculas.

### PowerShell no permite activar `myenv`

Ejecute:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

Después vuelva a ejecutar:

```powershell
.\myenv\Scripts\Activate.ps1
```

## Resumen de puertos

| Componente | Puerto | Uso |
|---|---:|---|
| API de pedidos | `8000` | HTTP y Swagger |
| MySQL | `3307` | Persistencia |
| RabbitMQ | `5672` | Mensajería AMQP |
| RabbitMQ Management | `15672` | Administración web |

