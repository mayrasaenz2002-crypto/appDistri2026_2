# MicroClientes.API

Microservicio desarrollado con **ASP.NET Core Web API** para la gestión de clientes.  
Implementa una arquitectura basada en capas, conexión con **SQL Server**, persistencia de datos mediante **Entity Framework Core** y despliegue utilizando **Docker**.

## Tecnologías utilizadas

- ASP.NET Core Web API
- C# .NET
- Entity Framework Core
- SQL Server 2022
- Docker
- Docker Compose
- Swagger OpenAPI

---

# Estructura del proyecto

```
app.microCliente.api
│
├── Controllers
│   └── ClientesController.cs
│
├── Data
│   └── AppDbContext.cs
│
├── Models
│   ├── Cliente.cs
│   ├── DireccionCliente.cs
│   └── EntityBase.cs
│
├── DTOs
│
├── Services
│
├── Repositories
│
├── Migrations
│
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

# Configuración de Base de Datos

La aplicación utiliza SQL Server ejecutándose dentro de un contenedor Docker.

Configuración utilizada:

```
Servidor: localhost,1434
Base de datos: MicroClientesDB
Usuario: sa
Puerto: 1434
```

La persistencia de información se mantiene mediante volúmenes Docker.

---

# Ejecución del proyecto

## Levantar servicios con Docker Compose

Ejecutar:

```bash
docker compose up -d --build
```

Esto crea y ejecuta:

- Contenedor de la API MicroClientes.
- Contenedor de SQL Server.
- Red interna Docker.
- Volumen para persistencia de datos.

---

# Acceso a Swagger

Una vez levantados los servicios, abrir:

```
http://localhost:8081/swagger
```

Desde Swagger se pueden probar los endpoints disponibles.

---

# Endpoints disponibles

## Obtener todos los clientes

```
GET /api/clientes
```

Consulta todos los clientes registrados.

---

## Obtener cliente por ID

```
GET /api/clientes/{id}
```

Consulta un cliente específico mediante su identificador.

Ejemplo:

```
GET /api/clientes/1
```

---

## Crear cliente

```
POST /api/clientes
```

Ejemplo de JSON:

```json
{
  "nombre": "Juan",
  "apellido": "Pérez",
  "cedula": "1723456789",
  "email": "juan@gmail.com",
  "telefono": "0999999999",
  "fechaNacimiento": "2000-01-15"
}
```

---

## Actualizar cliente

```
PUT /api/clientes/{id}
```

Permite modificar la información de un cliente existente.

---

## Eliminar cliente

```
DELETE /api/clientes/{id}
```

Elimina un cliente mediante su identificador.

---

# Persistencia de datos con Docker

Los datos se almacenan en un volumen Docker para evitar pérdida de información al detener los contenedores.

## Detener servicios

```bash
docker compose down
```

Este comando detiene los contenedores pero conserva los volúmenes.

## Importante

No utilizar:

```bash
docker compose down -v
```

porque elimina los volúmenes y provoca la pérdida de los datos almacenados en la base de datos.

---

# Dockerización del Microservicio Clientes

## Construir imagen

Permite crear la imagen Docker del microservicio:

```bash
docker build -t microclientes-api:1.0 .
```

---

## Levantar servicios

Construye y ejecuta los servicios definidos en Docker Compose:

```bash
docker compose up -d --build
```

---

## Ver contenedores

Permite visualizar los contenedores activos:

```bash
docker compose ps
```

También se puede utilizar:

```bash
docker ps
```

---

## Ver logs

Muestra los registros generados por el microservicio:

```bash
docker compose logs -f microclientes-api
```

---

## Detener servicios

Detiene los servicios activos:

```bash
docker compose down
```

---

## Reiniciar servicios

Reinicia los contenedores:

```bash
docker compose restart
```

---

## Ver imágenes

Lista las imágenes Docker disponibles:

```bash
docker images
```

---

## Ver volúmenes

Permite visualizar los volúmenes utilizados para almacenar información persistente:

```bash
docker volume ls
```

---

## Ver redes

Muestra las redes Docker creadas:

```bash
docker network ls
```

---

# Validación de persistencia

Para comprobar la persistencia de datos:

1. Crear clientes desde Swagger mediante:

```
POST /api/clientes
```

2. Consultar registros:

```
GET /api/clientes
```

3. Detener contenedores:

```bash
docker compose down
```

4. Levantar nuevamente:

```bash
docker compose up -d
```

5. Consultar nuevamente:

```
GET /api/clientes
```

Los registros deben mantenerse gracias al volumen Docker.

---

# Control de versiones

El proyecto utiliza Git para gestionar cambios.

Comandos utilizados:

```bash
git add .
git commit -m "mensaje del cambio"
git push
```

---

# Autor

Proyecto MicroClientes API  
Desarrollo de Software - Cuarto Semestre- Mayra Saenz_Eileen Angulo