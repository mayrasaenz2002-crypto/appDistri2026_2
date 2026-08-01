# Dockerización del eCommerce

Esta guía explica cómo dockerizar los siguientes procesos sin volver a crear ni modificar las bases de datos existentes:

- `app.microCliente.api`: API ASP.NET Core 8 conectada a SQL Server y RabbitMQ.
- `app.microPedidos.api`: API FastAPI conectada a MySQL y RabbitMQ.
- `app.microPedidos.worker`: consumidor Python de RabbitMQ conectado a MySQL.

Las bases de datos, RabbitMQ y Kong ya están dockerizados. Los nuevos contenedores se conectarán a la red Docker existente `netappdistri`.

## 1. Arquitectura y puertos

### Puertos directos y puertos internos

| Componente | Dirección desde Windows | Dirección dentro de Docker |
|---|---|---|
| MicroCliente API | `localhost:5180` | `app-microcliente-api:8081` |
| MicroPedidos API | `localhost:8002` | `app-micropedidos-api:8082` |
| MicroPedidos worker | No expone puerto | No expone puerto |
| SQL Server | `localhost:1434` | `database-sql:1433` |
| MySQL | `localhost:3307` | `database-mysql:3306` |
| RabbitMQ AMQP | `localhost:5672` | `component-event-rabbitmq:5672` |
| RabbitMQ Management | `localhost:15672` | `component-event-rabbitmq:15672` |
| Kong Proxy | `localhost:8000` | `gateway-kong:8000` |
| Kong Admin | `localhost:8001` | `gateway-kong:8001` |
| Konga | `localhost:1337` | `gateway-konga:1337` |

Los puertos no se repiten dentro del grupo de APIs:

- MicroCliente escucha dentro de Docker en `8081`.
- MicroPedidos escucha dentro de Docker en `8082`.
- Kong recibe todas las peticiones públicas en `8000`.
- Los puertos `5180` y `8002` permiten probar directamente las APIs desde Windows.

Un puerto del host y uno de un contenedor pertenecen a espacios diferentes. La expresión `5180:8081` significa: puerto `5180` de Windows dirigido al puerto `8081` del contenedor.

### Acceso común mediante Kong

Las URLs públicas comunes serán:

```text
http://localhost:8000/ecommerce/microclientes
http://localhost:8000/ecommerce/micropedidos
```

Los endpoints específicos se agregan después del prefijo. Por ejemplo:

```text
http://localhost:8000/ecommerce/microclientes/api/Cliente/obtener-todos
http://localhost:8000/ecommerce/micropedidos/pedidos
http://localhost:8000/ecommerce/micropedidos/login
```

## 2. Requisitos previos

1. Instalar y ejecutar Docker Desktop.
2. Confirmar que las bases y RabbitMQ estén iniciados:

   ```powershell
   docker ps
   ```

3. En la salida deben aparecer, al menos:

   ```text
   database-sql
   database-mysql
   component-event-rabbitmq
   ```

4. Confirmar que la red existente se llama `netappdistri`:

   ```powershell
   docker network inspect netappdistri
   ```

Si la red no existe, se puede crear una sola vez con:

```powershell
docker network create netappdistri
```

No se deben volver a crear las bases ni ejecutar scripts de inicialización, porque este entorno ya contiene las bases y tablas.

## 3. Regla de conectividad entre contenedores

Dentro de un contenedor, `localhost` identifica al mismo contenedor. Por tanto, las aplicaciones no deben conectarse a las bases utilizando los puertos publicados en Windows.

Se deben utilizar el nombre del contenedor y el puerto interno:

| Dependencia | Incorrecto dentro de Docker | Correcto dentro de Docker |
|---|---|---|
| SQL Server | `localhost,1434` | `database-sql,1433` |
| MySQL | `localhost:3307` | `database-mysql:3306` |
| RabbitMQ | `localhost:5672` | `component-event-rabbitmq:5672` |

Esto funciona porque todos los contenedores pertenecen a `netappdistri` y Docker proporciona resolución DNS por nombre.

## 4. Dockerizar MicroCliente

### 4.1 Crear `appsettings.Docker.json`

Crear el archivo:

```text
app.microCliente.api/app.microCliente.api/appsettings.Docker.json
```

Contenido:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "BDDSqlServer": "Server=database-sql,1433;Database=microCliente;User Id=sa;Password=adminAppDist2024#;TrustServerCertificate=True"
  },
  "rabbitmq": {
    "username": "admin",
    "password": "admin",
    "virtualHost": "/",
    "port": 5672,
    "hostname": "component-event-rabbitmq"
  }
}
```

ASP.NET Core carga automáticamente `appsettings.Docker.json` cuando la variable `ASPNETCORE_ENVIRONMENT` tiene el valor `Docker`.

La configuración local de `appsettings.json` puede conservar `localhost:1434`. De esta manera, el mismo proyecto sirve para ejecución local y para Docker.

### 4.2 Ajustar Swagger y HTTPS para Docker

En:

```text
app.microCliente.api/app.microCliente.api/Program.cs
```

Reemplazar:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
```

por:

```csharp
if (app.Environment.IsDevelopment() ||
    app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}
```

El contenedor atenderá HTTP en `8081`. Kong será el punto de entrada común y puede encargarse posteriormente de HTTPS.

No se debe agregar `Database.Migrate()` ni código para crear la base, porque SQL Server ya contiene la base y las tablas.

### 4.3 Crear el Dockerfile

Crear:

```text
app.microCliente.api/Dockerfile
```

Contenido:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore app.microCliente.api/app.microCliente.api.csproj

RUN dotnet publish \
    app.microCliente.api/app.microCliente.api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_HTTP_PORTS=8081

EXPOSE 8081

ENTRYPOINT ["dotnet", "app.microCliente.api.dll"]
```

El contexto de construcción debe ser `app.microCliente.api`, la carpeta que contiene la solución completa. Así Docker puede copiar y compilar las referencias a `services`, `dataAccess`, `entities` y `common`.

### 4.4 Crear `.dockerignore`

Crear:

```text
app.microCliente.api/.dockerignore
```

Contenido:

```text
**/bin
**/obj
.vs
.vscode
.idea
.git
*.user
```

## 5. Dockerizar MicroPedidos

La API y el worker usarán la misma imagen Python. Cada contenedor tendrá un comando de inicio diferente.

### 5.1 Crear `configDocker.py`

Crear:

```text
app.microPedidos.api/app/core/configDocker.py
```

Contenido:

```python
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
```

### 5.2 Seleccionar la configuración según el ambiente

Modificar:

```text
app.microPedidos.api/app/core/config.py
```

Contenido:

```python
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
```

No es necesario cambiar las importaciones de `database.py`, el productor o el consumidor. Todos continúan importando desde `app.core.config`.

Cuando `APP_ENV=docker`, `config.py` utiliza `configDocker.py`. Cuando el proyecto se ejecuta directamente en Windows, utiliza `localhost:3307` y `localhost:5672`.

### 5.3 Crear el Dockerfile

Crear:

```text
app.microPedidos.api/Dockerfile
```

Contenido:

```dockerfile
FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1
ENV APP_ENV=docker

WORKDIR /app

COPY requirements.txt .

RUN pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8082

CMD ["uvicorn", "main_api:app", "--host", "0.0.0.0", "--port", "8082"]
```

### 5.4 Crear `.dockerignore`

Crear:

```text
app.microPedidos.api/.dockerignore
```

Contenido:

```text
myenv
venv
.venv
__pycache__
*.pyc
*.pyo
.git
.vscode
.idea
```

## 6. Crear el Compose de las aplicaciones

Crear la carpeta y el archivo:

```text
dockerCompose/Applications/docker-compose-applications.yml
```

Contenido:

```yaml
services:
  microcliente-api:
    build:
      context: ../../app.microCliente.api
      dockerfile: Dockerfile
    image: ecommerce/microcliente-api:local
    container_name: app-microcliente-api
    restart: on-failure
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ASPNETCORE_HTTP_PORTS: "8081"
    ports:
      - "5180:8081"
    networks:
      - net-app-distri

  micropedidos-api:
    build:
      context: ../../app.microPedidos.api
      dockerfile: Dockerfile
    image: ecommerce/micropedidos:local
    container_name: app-micropedidos-api
    restart: on-failure
    environment:
      APP_ENV: docker
    command:
      - uvicorn
      - main_api:app
      - --host
      - 0.0.0.0
      - --port
      - "8082"
    ports:
      - "8002:8082"
    networks:
      - net-app-distri

  micropedidos-worker:
    build:
      context: ../../app.microPedidos.api
      dockerfile: Dockerfile
    image: ecommerce/micropedidos:local
    container_name: app-micropedidos-worker
    restart: on-failure
    environment:
      APP_ENV: docker
    command:
      - python
      - main_worker.py
    networks:
      - net-app-distri

networks:
  net-app-distri:
    external: true
    name: netappdistri
```

Puntos importantes:

- `5180:8081`: MicroCliente se prueba en `localhost:5180`, pero escucha en `8081` dentro del contenedor.
- `8002:8082`: MicroPedidos se prueba en `localhost:8002`, pero escucha en `8082` dentro del contenedor.
- El worker no declara `ports` porque no ofrece una API HTTP.
- `external: true` indica que Compose debe utilizar la red existente y no crear otra.
- No se usa `depends_on` porque las bases y RabbitMQ están definidos en otros proyectos Compose.

## 7. Validar el archivo Compose

Desde la raíz del repositorio ejecutar:

```powershell
docker compose `
  -f dockerCompose\Applications\docker-compose-applications.yml `
  config
```

Este comando valida la sintaxis y muestra la configuración final sin crear contenedores.

## 8. Construir las imágenes

Ejecutar:

```powershell
docker compose `
  -f dockerCompose\Applications\docker-compose-applications.yml `
  build
```

Comprobar las imágenes:

```powershell
docker images
```

Deben aparecer:

```text
ecommerce/microcliente-api
ecommerce/micropedidos
```

## 9. Iniciar los contenedores

Primero verificar que SQL Server, MySQL y RabbitMQ estén ejecutándose:

```powershell
docker ps
```

Después iniciar las aplicaciones:

```powershell
docker compose `
  -f dockerCompose\Applications\docker-compose-applications.yml `
  up -d
```

Verificar:

```powershell
docker ps
```

Deben aparecer:

```text
app-microcliente-api
app-micropedidos-api
app-micropedidos-worker
```

## 10. Revisar logs

MicroCliente:

```powershell
docker logs app-microcliente-api
```

MicroPedidos API:

```powershell
docker logs app-micropedidos-api
```

Worker:

```powershell
docker logs app-micropedidos-worker
```

Seguir el worker en tiempo real:

```powershell
docker logs -f app-micropedidos-worker
```

Si un contenedor termina, comprobar su estado y código de salida:

```powershell
docker ps -a
```

## 11. Probar directamente las APIs

### MicroCliente

Swagger:

```text
http://localhost:5180/swagger
```

Ejemplos:

```text
GET http://localhost:5180/api/Cliente/obtener-todos
GET http://localhost:5180/api/DireccionCliente/obtener-todos
```

### MicroPedidos

Swagger:

```text
http://localhost:8002/docs
```

Ejemplos:

```text
POST http://localhost:8002/login
GET  http://localhost:8002/pedidos
```

Los endpoints protegidos de MicroPedidos requieren el token JWT obtenido en `/login`.

## 12. Confirmar la red compartida

Ejecutar:

```powershell
docker network inspect netappdistri
```

En la sección `Containers` deben aparecer las bases, RabbitMQ, Kong y las aplicaciones. Entre otros:

```text
database-sql
database-mysql
component-event-rabbitmq
app-microcliente-api
app-micropedidos-api
app-micropedidos-worker
gateway-kong
```

Si Kong no aparece en esa red, revisar que su Compose también use la red con nombre `netappdistri`.

## 13. Configurar MicroCliente en Kong

Kong debe dirigir el prefijo `/ecommerce/microclientes` hacia el puerto interno `8081` de MicroCliente.

Crear el servicio:

```powershell
curl.exe -i -X POST http://localhost:8001/services `
  --data "name=ecommerce-microclientes-service" `
  --data "url=http://app-microcliente-api:8081"
```

Crear la ruta:

```powershell
curl.exe -i -X POST `
  http://localhost:8001/services/ecommerce-microclientes-service/routes `
  --data "name=ecommerce-microclientes-route" `
  --data "paths[]=/ecommerce/microclientes" `
  --data "strip_path=true"
```

Con `strip_path=true`, Kong elimina el prefijo antes de enviar la solicitud:

```text
Cliente:
http://localhost:8000/ecommerce/microclientes/api/Cliente/obtener-todos

Kong envía a:
http://app-microcliente-api:8081/api/Cliente/obtener-todos
```

## 14. Configurar MicroPedidos en Kong

Kong debe dirigir `/ecommerce/micropedidos` hacia el puerto interno `8082` de MicroPedidos.

Crear el servicio:

```powershell
curl.exe -i -X POST http://localhost:8001/services `
  --data "name=ecommerce-micropedidos-service" `
  --data "url=http://app-micropedidos-api:8082"
```

Crear la ruta:

```powershell
curl.exe -i -X POST `
  http://localhost:8001/services/ecommerce-micropedidos-service/routes `
  --data "name=ecommerce-micropedidos-route" `
  --data "paths[]=/ecommerce/micropedidos" `
  --data "strip_path=true"
```

Ejemplos del enrutamiento:

```text
Cliente:
http://localhost:8000/ecommerce/micropedidos/pedidos

Kong envía a:
http://app-micropedidos-api:8082/pedidos
```

```text
Cliente:
http://localhost:8000/ecommerce/micropedidos/login

Kong envía a:
http://app-micropedidos-api:8082/login
```

## 15. Verificar la configuración de Kong

Listar servicios:

```powershell
curl.exe http://localhost:8001/services
```

Listar rutas:

```powershell
curl.exe http://localhost:8001/routes
```

Probar las rutas públicas:

```powershell
curl.exe http://localhost:8000/ecommerce/microclientes/api/Cliente/obtener-todos
curl.exe http://localhost:8000/ecommerce/micropedidos/
```

El endpoint raíz de cada prefijo es:

```text
http://localhost:8000/ecommerce/microclientes
http://localhost:8000/ecommerce/micropedidos
```

MicroPedidos tiene implementado `GET /`, por lo que su URL raíz debe devolver el mensaje de funcionamiento. MicroCliente trabaja principalmente con rutas `api/[controller]`; por eso, en MicroCliente normalmente se agregará `/api/Cliente`, `/api/DireccionCliente` o `/api/Persona` después del prefijo común.

## 16. Flujo completo del sistema

1. El cliente llama a Kong por el puerto `8000`.
2. Kong reconoce el prefijo `/ecommerce/microclientes` o `/ecommerce/micropedidos`.
3. Kong elimina el prefijo porque la ruta tiene `strip_path=true`.
4. Kong envía la solicitud a `app-microcliente-api:8081` o `app-micropedidos-api:8082`.
5. MicroCliente consulta SQL Server mediante `database-sql:1433`.
6. MicroPedidos consulta MySQL mediante `database-mysql:3306`.
7. MicroCliente y MicroPedidos se comunican con RabbitMQ mediante `component-event-rabbitmq:5672`.
8. El worker consume `clienteDireccionEvent` y registra la información en MySQL.

## 17. Detener o reconstruir solamente las aplicaciones

Detener los tres contenedores de aplicación sin tocar las bases:

```powershell
docker compose `
  -f dockerCompose\Applications\docker-compose-applications.yml `
  down
```

Este comando no elimina SQL Server, MySQL, RabbitMQ ni sus datos porque pertenecen a otros proyectos Compose.

Después de modificar código, reconstruir e iniciar:

```powershell
docker compose `
  -f dockerCompose\Applications\docker-compose-applications.yml `
  up -d --build
```

## 18. Solución de problemas

### La aplicación intenta conectarse a `localhost`

Comprobar las variables de ambiente:

```powershell
docker inspect app-microcliente-api
docker inspect app-micropedidos-api
```

MicroCliente debe tener `ASPNETCORE_ENVIRONMENT=Docker` y MicroPedidos debe tener `APP_ENV=docker`.

### Error de conexión con SQL Server

Comprobar:

```powershell
docker logs database-sql
docker network inspect netappdistri
```

La cadena dentro de Docker debe utilizar:

```text
Server=database-sql,1433
```

### Error de conexión con MySQL

La URL dentro de Docker debe utilizar:

```text
mysql+mysqlconnector://root:admin@database-mysql:3306/microPedidos
```

### Error de conexión con RabbitMQ

El hostname debe ser:

```text
component-event-rabbitmq
```

Revisar:

```powershell
docker logs component-event-rabbitmq
docker logs app-micropedidos-worker
```

### Kong responde `502 Bad Gateway`

Verificar:

1. Que Kong pertenezca a `netappdistri`.
2. Que `app-microcliente-api` escuche en `8081`.
3. Que `app-micropedidos-api` escuche en `8082`.
4. Que los servicios Kong usen los nombres de contenedor, no `localhost`.

Comprobar los logs:

```powershell
docker logs gateway-kong
docker logs app-microcliente-api
docker logs app-micropedidos-api
```

### El puerto ya está ocupado en Windows

Comprobar los puertos publicados:

```powershell
docker ps --format "table {{.Names}}\t{{.Ports}}"
```

La distribución esperada es:

```text
5180  -> MicroCliente
8002  -> MicroPedidos
8000  -> Kong Proxy
8001  -> Kong Admin
1337  -> Konga
```

## 19. Resumen final

Acceso directo para desarrollo:

```text
MicroCliente: http://localhost:5180
MicroPedidos: http://localhost:8002
```

Acceso recomendado para consumidores mediante el API Gateway:

```text
MicroCliente: http://localhost:8000/ecommerce/microclientes
MicroPedidos: http://localhost:8000/ecommerce/micropedidos
```

Todos los consumidores utilizan un único host y puerto público, `localhost:8000`. Kong diferencia el microservicio por el prefijo de la URL.
