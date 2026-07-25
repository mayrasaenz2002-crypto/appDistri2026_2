# Aplicaciones Distribuidas

# README Teórico – Semana 3

## MicroPedidos, JWT, Docker, Postman y Docker Hub

**Carrera:** Desarrollo de Software
**Asignatura:** Aplicaciones Distribuidas
**Unidad:** Unidad 2 – Desarrollo, seguridad y despliegue de microservicios
**Semana:** 3
**Proyecto integrador:** MicroClientes + MicroPedidos

---

# 1. Propósito de la Semana 3

Durante la Semana 3 el estudiante consolidará el funcionamiento del microservicio **MicroPedidos** y comprenderá cómo proteger, probar, contenerizar y publicar una solución basada en microservicios.

Los principales conceptos teóricos de esta semana son:

* Lógica de negocio en un microservicio.
* Registro y cálculo de pedidos.
* DTO de eventos.
* Publicación de eventos mediante RabbitMQ.
* Seguridad con JWT.
* Autenticación y autorización.
* Imágenes y contenedores Docker.
* Dockerfile.
* Docker Compose.
* Pruebas de APIs con Postman.
* Ambientes de ejecución.
* Docker Hub.
* Versionamiento y distribución de imágenes.

---

# 2. Arquitectura trabajada en la semana

La práctica relaciona los siguientes componentes:

```mermaid
flowchart LR
    P[Postman] -->|HTTP| MC[MicroClientes]
    P -->|HTTP + JWT| MP[MicroPedidos API]

    MC --> SQL[(SQL Server)]
    MP --> MYSQL[(MySQL)]

    MC -->|clienteDireccionEvent| RMQ[RabbitMQ]
    RMQ -->|Consume| MW[MicroPedidos Worker]
    MW --> MYSQL

    MP -->|pedidoRegistradoEvent| RMQ
    RMQ -->|Semana 4| MF[MicroFactura]

    MC -. Imagen Docker .-> DH[Docker Hub]
    MP -. Imagen Docker .-> DH
```

La arquitectura combina comunicación síncrona mediante HTTP y comunicación asíncrona mediante eventos.

---

# 3. Responsabilidad del MicroPedidos

MicroPedidos es responsable de administrar los pedidos del sistema.

Sus funciones principales son:

* Recibir información de clientes y direcciones.
* Almacenar una copia local de ClienteDireccion.
* Registrar pedidos.
* Calcular el total.
* Proteger endpoints con JWT.
* Publicar el evento `pedidoRegistradoEvent`.
* Entregar información a MicroFactura mediante RabbitMQ.

---

# 4. Principio de responsabilidad única

Cada microservicio debe concentrarse en una responsabilidad específica del negocio.

```text
MicroClientes
Gestiona clientes y direcciones.

MicroPedidos
Gestiona pedidos.

MicroFactura
Gestiona facturas.
```

MicroPedidos no debe:

* Modificar clientes.
* Consultar directamente la base de datos de MicroClientes.
* Generar facturas.
* Compartir su base de datos con otros servicios.

Este principio reduce el acoplamiento y facilita el mantenimiento del sistema.

---

# 5. Base de datos independiente por microservicio

Cada microservicio posee su propia base de datos.

| Microservicio | Base de datos |
| ------------- | ------------- |
| MicroClientes | SQL Server    |
| MicroPedidos  | MySQL         |
| MicroFactura  | PostgreSQL    |

MicroPedidos no se conecta directamente a SQL Server para obtener información del cliente.

La información llega mediante un evento publicado por MicroClientes.

```mermaid
flowchart LR
    MC[MicroClientes] --> SQL[(SQL Server)]
    MC -->|Evento| RMQ[RabbitMQ]
    RMQ --> MP[MicroPedidos]
    MP --> MYSQL[(MySQL)]
```

Esta separación evita dependencias directas entre bases de datos.

---

# 6. Copia local de información externa

MicroPedidos recibe la información del cliente mediante el evento:

```text
clienteDireccionEvent
```

El Worker almacena una copia local en MySQL.

Ejemplo:

```text
ClienteDireccion
-------------------------
Id
ClienteId
NombreCompleto
Email
DireccionCompleta
FechaRegistro
```

Esta copia permite que MicroPedidos trabaje de forma independiente aunque MicroClientes no esté disponible temporalmente.

---

# 7. Lógica de negocio en PedidoService

La lógica de negocio no debe ubicarse directamente en el Controller.

Debe implementarse en una capa de servicios.

El `PedidoService` puede encargarse de:

* Validar el pedido.
* Buscar la información del cliente.
* Calcular el total.
* Guardar el pedido.
* Construir el DTO del evento.
* Publicar el evento en RabbitMQ.

```mermaid
flowchart TD
    C[Controller] --> S[PedidoService]
    S --> V[Validaciones]
    S --> DB[Persistencia]
    S --> CALC[Cálculo del total]
    S --> DTO[Construcción del DTO]
    S --> RMQ[Publicación RabbitMQ]
```

---

# 8. Cálculo del total del pedido

El total del pedido debe calcularse dentro del microservicio.

```text
Total = Cantidad × PrecioUnitario
```

Ejemplo:

```text
Cantidad: 2
PrecioUnitario: 25,50
Total: 51,00
```

El total no debería ser aceptado directamente desde el cliente como valor definitivo, porque podría ser manipulado o enviado incorrectamente.

El microservicio debe controlar esta regla de negocio.

---

# 9. Registro completo de un pedido

Cuando el usuario registra un pedido, MicroPedidos ejecuta varias acciones.

```mermaid
flowchart TD
    A[POST /pedidos] --> B[Validar JWT]
    B --> C[Validar datos]
    C --> D[Buscar ClienteDireccion]
    D --> E{Cliente encontrado}
    E -- No --> F[Devolver error]
    E -- Sí --> G[Calcular total]
    G --> H[Guardar pedido]
    H --> I[Construir evento]
    I --> J[Publicar pedidoRegistradoEvent]
    J --> K[Responder al cliente]
```

---

# 10. ¿Qué es un evento?

Un evento representa un hecho que ocurrió dentro del sistema.

Ejemplos:

* Cliente creado.
* Dirección registrada.
* Pedido registrado.
* Factura emitida.

En este proyecto:

```text
pedidoRegistradoEvent
```

significa:

> Un pedido fue registrado correctamente y está disponible para que otros microservicios lo procesen.

---

# 11. DTO de evento

Un DTO de evento transporta únicamente la información necesaria para otros microservicios.

MicroPedidos debe enviar un único objeto con la información del pedido, cliente y dirección.

Ejemplo:

```json
{
  "pedidoId": 1001,
  "clienteId": 15,
  "nombreCliente": "Carlos Pérez",
  "email": "carlos@email.com",
  "direccionEntrega": "Av. Amazonas y Naciones Unidas",
  "producto": "Laptop Lenovo",
  "cantidad": 1,
  "precioUnitario": 850.00,
  "total": 850.00,
  "estado": "REGISTRADO",
  "fechaPedido": "2026-07-21T20:30:00"
}
```

---

# 12. ¿Por qué enviar un solo objeto?

MicroFactura necesita conocer:

* Qué pedido se registró.
* Quién es el cliente.
* A qué dirección se entregará.
* Qué producto se compró.
* Cuál es el total.

Al enviar toda la información necesaria en un único DTO, MicroFactura no tiene que consultar directamente a MicroClientes ni a MicroPedidos.

Esto reduce el acoplamiento entre servicios.

---

# 13. Comunicación síncrona y asíncrona

## Comunicación síncrona

La comunicación síncrona ocurre cuando una aplicación envía una solicitud y espera una respuesta.

Ejemplo:

```text
Postman → MicroPedidos → Respuesta HTTP
```

Protocolos comunes:

* HTTP.
* HTTPS.
* REST.

## Comunicación asíncrona

La comunicación asíncrona ocurre cuando un servicio publica un mensaje y continúa trabajando sin esperar una respuesta inmediata.

Ejemplo:

```text
MicroPedidos → RabbitMQ → MicroFactura
```

```mermaid
flowchart LR
    P[Publisher] -->|Publica evento| RMQ[RabbitMQ]
    RMQ -->|Entrega evento| S[Subscriber]
```

---

# 14. RabbitMQ como Message Broker

RabbitMQ es un intermediario de mensajes.

Sus responsabilidades son:

* Recibir mensajes.
* Enrutarlos.
* Mantenerlos en una cola.
* Entregarlos a los consumidores.
* Facilitar la comunicación entre aplicaciones.

```mermaid
flowchart LR
    MP[MicroPedidos] -->|pedidoRegistradoEvent| EX[Exchange]
    EX --> Q[Queue]
    Q --> MF[MicroFactura]
```

MicroPedidos funciona como Publisher y MicroFactura como Subscriber.

---

# 15. Publisher y Subscriber

## Publisher

Es el componente que publica el evento.

En esta semana:

```text
MicroPedidos
```

publica:

```text
pedidoRegistradoEvent
```

## Subscriber

Es el componente que consume el evento.

En la Semana 4:

```text
MicroFactura
```

consumirá el evento.

---

# 16. Ventajas de la mensajería asíncrona

La mensajería asíncrona aporta:

* Bajo acoplamiento.
* Tolerancia a fallos.
* Procesamiento en segundo plano.
* Escalabilidad.
* Independencia tecnológica.
* Mayor resiliencia.
* Comunicación entre servicios heterogéneos.

Si MicroFactura está temporalmente apagado, RabbitMQ puede mantener el mensaje hasta que el consumidor vuelva a estar disponible.

---

# 17. Seguridad en APIs

Una API expuesta sin seguridad puede ser consumida por cualquier usuario.

La seguridad permite controlar:

* Quién accede.
* Qué operaciones puede realizar.
* Qué recursos puede consultar.
* Qué endpoints requieren autenticación.

Durante esta semana se utiliza JWT para proteger MicroPedidos.

---

# 18. ¿Qué es JWT?

JWT significa:

```text
JSON Web Token
```

Es un estándar para transmitir información firmada entre un cliente y un servidor.

Un JWT tiene tres partes:

```text
Header.Payload.Signature
```

Ejemplo conceptual:

```text
eyJhbGciOiJIUzI1NiJ9
.
eyJzdWIiOiJhZG1pbkBlbWFpbC5jb20ifQ
.
firma-digital
```

---

# 19. Estructura de un JWT

## Header

Indica el algoritmo utilizado.

```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

## Payload

Contiene datos o claims.

```json
{
  "sub": "admin@gmail.com",
  "role": "admin",
  "exp": 1784554200
}
```

## Signature

Permite verificar que el token no fue modificado.

---

# 20. JWT firmado no significa cifrado

El contenido del JWT puede ser decodificado.

Por ello, nunca se debe guardar información sensible dentro del payload.

No se debe incluir:

* Contraseñas.
* Números de tarjetas.
* Secretos.
* Información confidencial.

La implementación actual del proyecto incluye la contraseña en el payload y utiliza un secreto escrito directamente en el código, por lo que debe considerarse una implementación educativa que requiere mejoras antes de utilizarse en producción.

---

# 21. Autenticación y autorización

## Autenticación

Responde a la pregunta:

```text
¿Quién es el usuario?
```

Ejemplo:

```text
POST /login
```

## Autorización

Responde a la pregunta:

```text
¿Qué puede hacer el usuario?
```

Ejemplo:

```text
Solo el usuario con rol administrador puede eliminar pedidos.
```

JWT puede utilizarse para apoyar ambos procesos.

---

# 22. Flujo de autenticación JWT

```mermaid
sequenceDiagram
    participant U as Usuario
    participant L as Login
    participant J as JWT Manager
    participant API as Endpoint protegido

    U->>L: Email y contraseña
    L->>L: Validar credenciales
    L->>J: Crear token
    J-->>L: JWT firmado
    L-->>U: Token

    U->>API: Authorization Bearer Token
    API->>J: Validar token
    J-->>API: Token válido
    API-->>U: Acceso permitido
```

---

# 23. Bearer Token

El token se envía en el encabezado HTTP:

```http
Authorization: Bearer <token>
```

Ejemplo:

```http
GET /pedidos
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

La palabra `Bearer` indica que el usuario presenta un token como credencial.

---

# 24. Dependencias de seguridad en FastAPI

FastAPI permite proteger endpoints mediante dependencias.

Ejemplo conceptual:

```python
dependencies=[Depends(JWTBearerToken())]
```

La dependencia:

* Lee el encabezado Authorization.
* Extrae el token.
* Valida la firma.
* Decodifica el payload.
* Verifica el usuario.
* Permite o rechaza el acceso.

La implementación actual protege `GET /pedidos` y `POST /pedidos`, mientras que los demás endpoints requieren agregar la dependencia o proteger el router completo.

---

# 25. Respuestas HTTP relacionadas con seguridad

| Código | Significado                             |
| -----: | --------------------------------------- |
|  `200` | Solicitud correcta                      |
|  `201` | Recurso creado                          |
|  `400` | Solicitud incorrecta                    |
|  `401` | Usuario no autenticado                  |
|  `403` | Usuario autenticado, pero no autorizado |
|  `404` | Recurso no encontrado                   |
|  `500` | Error interno                           |

---

# 26. Buenas prácticas JWT

Una implementación adecuada debe:

* Guardar el secreto en variables de entorno.
* No incluir contraseñas en el token.
* Configurar fecha de expiración.
* Validar el algoritmo.
* Manejar tokens inválidos.
* Usar claims como `sub`, `role`, `iat` y `exp`.
* Utilizar HTTPS.
* Rotar secretos cuando sea necesario.

Variables recomendadas:

```text
JWT_SECRET
JWT_ALGORITHM
JWT_EXPIRATION_MINUTES
```

---

# 27. ¿Qué es Docker?

Docker es una plataforma que permite empaquetar una aplicación y sus dependencias dentro de un contenedor.

El contenedor puede ejecutarse de forma consistente en distintos ambientes.

```mermaid
flowchart LR
    C[Código fuente] --> D[Dockerfile]
    D --> I[Imagen]
    I --> CT[Contenedor]
```

---

# 28. Problema que resuelve Docker

Sin Docker pueden presentarse diferencias como:

* Distintas versiones de Python.
* Distintas versiones de .NET.
* Dependencias faltantes.
* Configuraciones diferentes.
* Problemas de puertos.
* Problemas de sistema operativo.

Docker permite ejecutar la aplicación en un ambiente controlado.

---

# 29. Imagen Docker

Una imagen es una plantilla inmutable utilizada para crear contenedores.

Contiene:

* Sistema base.
* Runtime.
* Dependencias.
* Código.
* Configuración de inicio.

Ejemplos:

```text
microclientes:1.0
micropedidos:1.0
```

---

# 30. Contenedor Docker

Un contenedor es una instancia en ejecución de una imagen.

```text
Imagen
↓
Contenedor 1
Contenedor 2
Contenedor 3
```

Una misma imagen puede crear varios contenedores.

Esto se aplica en MicroPedidos:

```text
Imagen MicroPedidos
├── Contenedor API
└── Contenedor Worker
```

---

# 31. Dockerfile

El Dockerfile contiene las instrucciones para construir una imagen.

Ejemplo conceptual:

```dockerfile
FROM python:3.11

WORKDIR /app

COPY requirements.txt .

RUN pip install -r requirements.txt

COPY . .

CMD ["uvicorn", "main_api:app", "--host", "0.0.0.0"]
```

Conceptos principales:

* `FROM`: imagen base.
* `WORKDIR`: carpeta de trabajo.
* `COPY`: copiar archivos.
* `RUN`: ejecutar comandos.
* `EXPOSE`: documentar puerto.
* `CMD`: comando de inicio.

---

# 32. Construcción de una imagen

El comando:

```bash
docker build -t micropedidos:1.0 .
```

realiza lo siguiente:

1. Lee el Dockerfile.
2. Descarga la imagen base.
3. Instala dependencias.
4. Copia el código.
5. Crea una imagen local.
6. Asigna un nombre y una versión.

---

# 33. Ejecución de un contenedor

Ejemplo:

```bash
docker run -d -p 8000:8000 micropedidos:1.0
```

Elementos:

* `docker run`: crea y ejecuta el contenedor.
* `-d`: ejecuta en segundo plano.
* `-p`: publica un puerto.
* `8000:8000`: puerto externo e interno.
* `micropedidos:1.0`: imagen utilizada.

---

# 34. Puerto interno y puerto externo

Ejemplo:

```text
8081:8080
```

Significa:

```text
Puerto del equipo: 8081
Puerto del contenedor: 8080
```

```mermaid
flowchart LR
    U[Usuario] -->|localhost:8081| H[Equipo host]
    H -->|Puerto 8080| C[Contenedor]
```

---

# 35. Variables de entorno

Las variables de entorno permiten configurar una aplicación sin modificar el código.

Ejemplos:

```text
DATABASE_URL
RABBITMQ_HOST
JWT_SECRET
JWT_ALGORITHM
```

Son útiles para configurar ambientes:

* Local.
* Docker.
* Pruebas.
* Producción.

---

# 36. Dockerización de MicroPedidos

MicroPedidos posee dos procesos:

* API.
* Worker.

Ambos pueden utilizar la misma imagen, pero deben ejecutarse en contenedores diferentes.

```mermaid
flowchart TD
    IMG[Imagen MicroPedidos] --> API[Contenedor API]
    IMG --> W[Contenedor Worker]

    API --> MYSQL[(MySQL)]
    W --> MYSQL
    W --> RMQ[RabbitMQ]
    API --> RMQ
```

Esto se debe a que cada contenedor debería ejecutar un proceso principal.

---

# 37. Docker Compose

Docker Compose permite definir y ejecutar varios contenedores mediante un archivo YAML.

Ejemplo conceptual:

```yaml
services:

  micropedidos-api:
    image: micropedidos:1.0

  micropedidos-worker:
    image: micropedidos:1.0

  mysql:
    image: mysql

  rabbitmq:
    image: rabbitmq
```

---

# 38. Ventajas de Docker Compose

Docker Compose permite:

* Levantar varios servicios.
* Configurar redes.
* Definir puertos.
* Crear variables de entorno.
* Configurar volúmenes.
* Controlar dependencias.
* Reproducir la arquitectura.

Comando principal:

```bash
docker compose up -d
```

---

# 39. Redes Docker

Una red Docker permite que los contenedores se comuniquen por nombre.

Ejemplo:

```text
mysql
rabbitmq
microclientes-api
micropedidos-api
micropedidos-worker
```

Dentro de la red no se recomienda utilizar `localhost` para conectarse a otro contenedor.

Debe utilizarse el nombre del servicio.

```text
mysql:3306
rabbitmq:5672
```

---

# 40. Persistencia y volúmenes

Los contenedores pueden eliminarse.

Para evitar perder información se utilizan volúmenes.

```mermaid
flowchart LR
    C[Contenedor MySQL] --> V[(Volumen)]
```

El volumen conserva:

* Bases de datos.
* Archivos.
* Configuraciones.
* Información persistente.

---

# 41. ¿Qué es Postman?

Postman es una herramienta para diseñar, ejecutar y probar APIs.

Permite:

* Enviar solicitudes HTTP.
* Probar endpoints.
* Configurar headers.
* Enviar JSON.
* Guardar tokens.
* Crear colecciones.
* Crear ambientes.
* Automatizar pruebas.

---

# 42. Colecciones en Postman

Una colección agrupa solicitudes relacionadas.

Ejemplo:

```text
Proyecto Integrador
├── Seguridad
├── MicroClientes
└── MicroPedidos
```

Las colecciones facilitan:

* Organización.
* Reutilización.
* Trabajo en equipo.
* Documentación.
* Ejecución de pruebas.

---

# 43. Ambientes en Postman

Un ambiente contiene variables utilizadas por las solicitudes.

Ejemplo:

```text
base_url_clientes
base_url_pedidos
token_pedidos
cliente_id
pedido_id
```

La misma solicitud puede ejecutarse en distintos ambientes sin modificar manualmente la URL.

---

# 44. Ambiente local

El ambiente local se utiliza cuando los proyectos se ejecutan directamente desde el código.

Ejemplo:

```text
base_url_clientes = https://localhost:7001
base_url_pedidos = http://localhost:8000
```

---

# 45. Ambiente Docker

El ambiente Docker se utiliza cuando las APIs se ejecutan dentro de contenedores.

Ejemplo:

```text
base_url_clientes = http://localhost:8081
base_url_pedidos = http://localhost:8000
```

Los puertos pueden cambiar según la configuración realizada.

---

# 46. Ambiente Gateway

El ambiente Gateway se utiliza cuando las solicitudes pasan por Kong.

Ejemplo:

```text
base_url_gateway = http://localhost:8001
```

Solicitudes:

```text
{{base_url_gateway}}/clientes
{{base_url_gateway}}/pedidos
```

El cliente no necesita conocer directamente la dirección interna de cada microservicio.

---

# 47. Ventaja de los ambientes

```mermaid
flowchart TD
    C[Colección Postman] --> L[Ambiente Local]
    C --> D[Ambiente Docker]
    C --> G[Ambiente Gateway]
```

La colección permanece igual.

Solo cambian las variables del ambiente.

---

# 48. Automatización del token en Postman

Después del login, Postman puede guardar automáticamente el token.

Ejemplo:

```javascript
const token = pm.response.json();
pm.environment.set("token_pedidos", token);
```

Luego se utiliza:

```text
Bearer {{token_pedidos}}
```

Esto evita copiar y pegar el token manualmente.

---

# 49. Flujo de prueba con Postman

```mermaid
flowchart TD
    A[Seleccionar ambiente] --> B[Ejecutar login]
    B --> C[Guardar JWT]
    C --> D[Crear cliente]
    D --> E[Crear dirección]
    E --> F[Verificar evento]
    F --> G[Crear pedido]
    G --> H[Verificar total]
    H --> I[Verificar pedidoRegistradoEvent]
```

---

# 50. ¿Qué es Docker Hub?

Docker Hub es un repositorio remoto para almacenar y distribuir imágenes Docker.

Es similar a GitHub, pero almacena imágenes de contenedores en lugar de código fuente.

Ejemplo:

```text
usuario/microclientes:1.0
usuario/micropedidos:1.0
```

---

# 51. Repositorio de Docker Hub

Un repositorio puede contener varias versiones de una imagen.

Ejemplo:

```text
usuario/micropedidos:1.0
usuario/micropedidos:1.1
usuario/micropedidos:latest
```

Cada versión se identifica mediante un tag.

---

# 52. Tags de imágenes

Un tag permite versionar una imagen.

Ejemplos:

```text
1.0
1.1
2.0
latest
```

No se recomienda depender únicamente de `latest`, porque no indica claramente qué versión se está utilizando.

---

# 53. Flujo de publicación en Docker Hub

```mermaid
flowchart LR
    C[Código] --> DF[Dockerfile]
    DF --> B[docker build]
    B --> I[Imagen local]
    I --> T[docker tag]
    T --> L[docker login]
    L --> P[docker push]
    P --> DH[Docker Hub]
    DH --> PL[docker pull]
```

---

# 54. Comandos principales de Docker Hub

Iniciar sesión:

```bash
docker login
```

Etiquetar:

```bash
docker tag micropedidos:1.0 usuario/micropedidos:1.0
```

Publicar:

```bash
docker push usuario/micropedidos:1.0
```

Descargar:

```bash
docker pull usuario/micropedidos:1.0
```

---

# 55. Docker Hub y trabajo en equipo

Docker Hub permite que:

* El docente publique imágenes.
* Los estudiantes descarguen una versión común.
* Un compañero ejecute la aplicación sin compilarla.
* Docker Compose descargue las imágenes.
* Se distribuyan versiones del proyecto.

Esto facilita la reproducibilidad de la arquitectura.

---

# 56. Diferencia entre GitHub y Docker Hub

| Característica         | GitHub                 | Docker Hub                   |
| ---------------------- | ---------------------- | ---------------------------- |
| Contenido principal    | Código fuente          | Imágenes Docker              |
| Unidad de publicación  | Repositorio Git        | Repositorio de imágenes      |
| Comando de descarga    | `git clone`            | `docker pull`                |
| Comando de publicación | `git push`             | `docker push`                |
| Objetivo               | Colaboración de código | Distribución de contenedores |

---

# 57. Integración conceptual de la semana

La Semana 3 integra varias áreas del desarrollo de software.

```mermaid
flowchart TD
    A[MicroPedidos] --> B[Lógica de negocio]
    A --> C[JWT]
    A --> D[RabbitMQ]
    A --> E[Docker]
    A --> F[Postman]
    A --> G[Docker Hub]

    B --> H[Registrar pedido]
    C --> I[Proteger endpoints]
    D --> J[Publicar evento]
    E --> K[Crear contenedores]
    F --> L[Probar APIs]
    G --> M[Compartir imágenes]
```

---

# 58. Competencias que debe desarrollar el estudiante

Al finalizar la Semana 3, el estudiante debe ser capaz de:

* Explicar la responsabilidad de MicroPedidos.
* Implementar reglas de negocio.
* Calcular el total de un pedido.
* Construir un DTO de evento.
* Publicar eventos mediante RabbitMQ.
* Diferenciar autenticación y autorización.
* Explicar la estructura de un JWT.
* Proteger endpoints en FastAPI.
* Crear imágenes Docker.
* Ejecutar contenedores.
* Diferenciar imagen y contenedor.
* Configurar puertos y variables.
* Utilizar Docker Compose.
* Crear colecciones en Postman.
* Configurar ambientes.
* Automatizar el uso de tokens.
* Publicar imágenes en Docker Hub.
* Descargar y ejecutar imágenes remotas.

---

# 59. Preguntas de repaso

1. ¿Cuál es la responsabilidad principal de MicroPedidos?
2. ¿Por qué MicroPedidos no consulta directamente la base de datos de MicroClientes?
3. ¿Qué información debe contener `pedidoRegistradoEvent`?
4. ¿Por qué el total debe calcularse en el servicio?
5. ¿Cuál es la diferencia entre un Publisher y un Subscriber?
6. ¿Cuál es la diferencia entre comunicación síncrona y asíncrona?
7. ¿Qué problema resuelve JWT?
8. ¿Cuál es la diferencia entre autenticación y autorización?
9. ¿Por qué no se debe incluir la contraseña en un JWT?
10. ¿Cuál es la diferencia entre una imagen y un contenedor?
11. ¿Por qué MicroPedidos necesita dos contenedores?
12. ¿Qué función cumple Docker Compose?
13. ¿Por qué no se usa `localhost` entre contenedores?
14. ¿Qué ventaja ofrecen los ambientes en Postman?
15. ¿Cuál es la diferencia entre GitHub y Docker Hub?
16. ¿Qué función cumplen los tags de Docker?
17. ¿Qué sucede si MicroFactura está apagado cuando se publica el evento?
18. ¿Por qué cada microservicio debe tener su propia base de datos?

---

# 60. Conclusión

La Semana 3 representa una etapa de consolidación del proyecto integrador.

MicroPedidos deja de ser únicamente una API CRUD y se convierte en un componente distribuido que:

* Recibe información mediante eventos.
* Aplica reglas de negocio.
* Protege sus endpoints.
* Publica mensajes para otros servicios.
* Se ejecuta dentro de contenedores.
* Se prueba en varios ambientes.
* Se distribuye mediante Docker Hub.

Estos conceptos preparan al estudiante para la Semana 4, donde MicroFactura consumirá `pedidoRegistradoEvent` y completará el flujo de negocio desde el registro del cliente hasta la generación de la factura.
