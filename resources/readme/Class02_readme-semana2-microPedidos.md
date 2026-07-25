# app.microPedidos.api

# Microservicio de Pedidos con FastAPI, MySQL y RabbitMQ

## Descripción

`app.microPedidos.api` es el segundo microservicio del proyecto integrador de la asignatura **Aplicaciones Distribuidas**.

Su responsabilidad es administrar los pedidos del sistema y servir como puente entre **MicroClientes** y **MicroFacturas** mediante comunicación asíncrona utilizando RabbitMQ.

Este microservicio posee dos procesos completamente independientes:

- Una **API REST** desarrollada con FastAPI para administrar los pedidos.
- Un **Worker RabbitMQ** encargado de consumir los eventos publicados por MicroClientes.

Ambos procesos comparten la misma base de datos, modelos y servicios, pero se ejecutan de forma independiente.

---

# Arquitectura General

```text
                   app.microCliente.api

                          │

                          │
        ClienteDireccionEventDto (RabbitMQ)

                          │

                          ▼

                  RabbitMQ Exchange

                          │

                          ▼

                 app.microPedidos.api

          ┌────────────────────────────────┐
          │                                │
          │       main_api.py              │
          │    (FastAPI REST API)          │
          │                                │
          └────────────────────────────────┘
                         │
                         │
                CRUD de Pedidos
                         │
                         ▼
                      MySQL
                         ▲
                         │
          ┌────────────────────────────────┐
          │                                │
          │      main_worker.py            │
          │  (Consumer RabbitMQ)           │
          │                                │
          └────────────────────────────────┘
                         ▲
                         │
              ClienteDireccionEvent

                         │
                     RabbitMQ

                         │

                         ▼

          PedidoService publica

          PedidoRegistradoEvent

                         │

                         ▼

                app.microFacturas.api
```

---

# Arquitectura del Proyecto

```text
app.microPedidos.api/

│

├── main_api.py

├── main_worker.py

├── requirements.txt

│

└── app/

    ├── api/

    │      routes.py

    │

    ├── core/

    │      config.py

    │      database.py

    │      rabbitmq_producer.py

    │

    ├── models/

    │      models.py

    │

    ├── schemas/

    │      schemas.py

    │

    ├── services/

    │      generic_service.py

    │      pedido_service.py

    │      authService.py

    │      jwt_manager.py

    │

    └── worker/

           consumer.py
```

---

# Tecnologías

- Python 3.11
- FastAPI
- SQLAlchemy
- MySQL
- RabbitMQ
- Pydantic
- Uvicorn
- Docker

---

# Base de Datos

Crear la base de datos.

```sql
CREATE DATABASE BddMicroPedidos;
```

---

# Crear el ambiente virtual

Ubicarse en la carpeta del proyecto.

```bash
cd app.microPedidos.api
```

Crear el ambiente virtual.

### Windows

```bash
python -m venv myenv
```

### Linux / Mac

```bash
python3 -m venv myenv
```

---

# Activar el ambiente virtual

## Windows

```bash
myenv\Scripts\activate
```

## Linux

```bash
source myenv/bin/activate
```

Cuando el ambiente está activo aparecerá algo similar a:

```text
(myenv)
```

---

# Instalar las dependencias

Actualizar pip

```bash
python -m pip install --upgrade pip
```

Instalar todas las dependencias

```bash
pip install -r requirements.txt
```

Si aún no existe el archivo requirements.txt

```bash
pip freeze > requirements.txt
```

---

# Dependencias principales

```text
fastapi

uvicorn

sqlalchemy

mysql-connector-python

pydantic

pika

python-jose

passlib

python-multipart
```

---

# Configuración

Editar

```text
app/core/config.py
```

Configurar

## MySQL

```python
DATABASE_URL =
"mysql+mysqlconnector://root:admin@localhost:3307/microPedidos"
```

## RabbitMQ

```python
HOST

PORT

USERNAME

PASSWORD

QUEUE

clienteDireccionEvent

pedidoRegistradoEvent
```

---

# Crear las tablas

El proyecto utiliza SQLAlchemy Code First.

Al iniciar la aplicación se ejecuta

```python
Base.metadata.create_all(bind=engine)
```

Las tablas se crearán automáticamente.

---

# Levantar la API

Abrir una terminal.

Activar el ambiente virtual.

```bash
myenv\Scripts\activate
```

Ejecutar

```bash
uvicorn main_api:app --reload
```

La API estará disponible en

```text
http://localhost:8000
```

Swagger

```text
http://localhost:8000/docs
```

---

# Levantar el Worker RabbitMQ

Abrir una segunda terminal.

Activar nuevamente el ambiente virtual.

```bash
myenv\Scripts\activate
```

Ejecutar

```bash
python main_worker.py
```

El Worker permanecerá escuchando continuamente la cola.

```text
clienteDireccionEvent
```

---

# ¿Por qué existen dos entradas?

Este proyecto posee dos responsabilidades completamente diferentes.

## Entrada 1

```text
main_api.py
```

Responsabilidad

- Exponer endpoints REST.
- Administrar pedidos.
- Publicar eventos.

---

## Entrada 2

```text
main_worker.py
```

Responsabilidad

- Escuchar RabbitMQ.
- Consumir ClienteDireccionEvent.
- Guardar ClienteDireccion.

---

Mientras la API espera solicitudes HTTP, el Worker espera mensajes RabbitMQ.

Ambos procesos deben ejecutarse simultáneamente.

---

# Flujo de funcionamiento

## Paso 1

MicroClientes registra un cliente.

↓

## Paso 2

Registra una dirección.

↓

## Paso 3

Publica

```text
clienteDireccionEvent
```

↓

## Paso 4

RabbitMQ recibe el evento.

↓

## Paso 5

main_worker.py consume el mensaje.

↓

## Paso 6

Se almacena ClienteDireccion.

↓

## Paso 7

El usuario crea un pedido desde la API.

↓

## Paso 8

PedidoService guarda el pedido.

↓

## Paso 9

PedidoService publica

```text
pedidoRegistradoEvent
```

↓

## Paso 10

MicroFacturas consumirá el evento.

---

# Arquitectura Interna

```text
Cliente HTTP

        │

        ▼

main_api.py

        │

        ▼

PedidoController

        │

        ▼

PedidoService

        │

        ├───────────────► SQLAlchemy

        │

        ├───────────────► MySQL

        │

        └───────────────► RabbitMQ
                             │
                             ▼
                    pedidoRegistradoEvent
```

---

# Arquitectura del Worker

```text
RabbitMQ

      │

      ▼

clienteDireccionEvent

      │

      ▼

main_worker.py

      │

      ▼

Consumer

      │

      ▼

GenericService

      │

      ▼

ClienteDireccion

      │

      ▼

MySQL
```

---

# Endpoints

## ClienteDireccion

No posee endpoints.

La información llega únicamente desde RabbitMQ.

---

## Pedidos

```http
GET /api/pedidos

GET /api/pedidos/{id}

POST /api/pedidos

PUT /api/pedidos/{id}

DELETE /api/pedidos/{id}
```

---

# Comunicación con RabbitMQ

## Consume

```text
clienteDireccionEvent
```

DTO recibido

```text
ClienteId

NombreCompleto

Email

DireccionCompleta
```

---

## Publica

```text
pedidoRegistradoEvent
```

DTO publicado

```text
PedidoId

ClienteId

NombreCliente

Producto

Cantidad

PrecioUnitario

Total
```

---

# Ejecución completa

Abrir dos terminales.

## Terminal 1

```bash
myenv\Scripts\activate

uvicorn main_api:app --reload
```

---

## Terminal 2

```bash
myenv\Scripts\activate

python main_worker.py
```

---

# Resultado esperado

Al finalizar la práctica el estudiante tendrá:

- Ambiente virtual configurado.
- Dependencias instaladas.
- MySQL configurado.
- RabbitMQ configurado.
- API FastAPI funcionando.
- Worker RabbitMQ funcionando.
- Arquitectura de doble entrada comprendida.
- CRUD de Pedidos operativo.
- Consumo del evento `clienteDireccionEvent`.
- Publicación del evento `pedidoRegistradoEvent`.

Este microservicio constituye el componente central de la arquitectura distribuida del proyecto integrador, actuando como intermediario entre **MicroClientes** y **MicroFacturas** mediante comunicación asíncrona basada en eventos.