# Seguridad JWT de `app.microPedidos.api`

Este documento explica cómo el proyecto genera, valida y utiliza tokens JWT para proteger los endpoints de la API de pedidos.

## Flujo de seguridad

```mermaid
sequenceDiagram
    participant Client as Cliente HTTP
    participant Login as POST /login
    participant JWT as JWT Manager
    participant API as Endpoint protegido
    participant Bearer as JWTBearerToken

    Client->>Login: Email y contraseña
    Login->>Login: Validar credenciales
    Login->>JWT: create_token(datos del usuario)
    JWT-->>Login: Token JWT firmado con HS256
    Login-->>Client: Token JWT

    Client->>API: Authorization: Bearer token
    API->>Bearer: Validar dependencia de seguridad
    Bearer->>JWT: validate_token(token)
    JWT-->>Bearer: Payload decodificado
    Bearer->>Bearer: Verificar email autorizado
    Bearer-->>API: Acceso permitido
    API-->>Client: Respuesta del endpoint
```

El flujo tiene tres partes:

1. El cliente obtiene un token mediante `POST /login`.
2. La API valida el token recibido en el encabezado `Authorization`.
3. FastAPI permite ejecutar solamente los endpoints que tengan la dependencia `JWTBearerToken`.

## 1. Generación del token

El endpoint de autenticación está definido en `main_api.py`:

```python
@app.post('/login', tags=['seguridad'])
def login(user: Usuario):
    if user.email == "admin@gmail.com" and user.password == "admin":
        token: str = create_token(user.model_dump())
        return JSONResponse(status_code=200, content=token)
```

El cliente debe enviar una solicitud como la siguiente:

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

El cuerpo se valida mediante el esquema `Usuario`, definido en `app/schemas/schemas.py`:

```python
class Usuario(BaseModel):
    email: str
    password: str
```

Si las credenciales son correctas, el endpoint invoca `create_token()` y devuelve el JWT al cliente.

## 2. Creación y firma del JWT

La generación del token se encuentra en `app/services/jwt_manager.py`:

```python
from jwt import decode, encode

codigoSecreto = "my_secrete_key_app_distri"

def create_token(data: dict) -> str:
    token: str = encode(
        payload=data,
        key=codigoSecreto,
        algorithm="HS256"
    )
    return token
```

El token contiene actualmente los datos recibidos durante el login:

```json
{
  "email": "admin@gmail.com",
  "password": "admin"
}
```

La firma usa:

- Algoritmo: `HS256`.
- Secreto: `my_secrete_key_app_distri`.
- Biblioteca: `PyJWT`.

El JWT está firmado, pero su contenido no está cifrado. Cualquier persona que tenga el token puede decodificar su payload. Por este motivo, una contraseña nunca debería incluirse dentro del token.

## 3. Envío del token

Para acceder a un endpoint protegido, el cliente debe incluir el token en el encabezado HTTP `Authorization`:

```http
Authorization: Bearer <token-jwt>
```

Ejemplo:

```http
GET /pedidos HTTP/1.1
Host: localhost:8000
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

En Swagger UI se puede usar el botón **Authorize** para registrar el Bearer token antes de ejecutar los endpoints protegidos.

## 4. Validación del token

La decodificación y validación criptográfica se realiza en `app/services/jwt_manager.py`:

```python
def validate_token(token: str) -> dict:
    data: dict = decode(
        token,
        key=codigoSecreto,
        algorithms=["HS256"]
    )
    return data
```

`PyJWT` comprueba que el token haya sido firmado con el mismo secreto y algoritmo. Si el token fue modificado o la firma no coincide, la decodificación genera una excepción.

## 5. Dependencia Bearer de FastAPI

La integración entre JWT y FastAPI está implementada en `app/services/authService.py`:

```python
class JWTBearerToken(HTTPBearer):
    async def __call__(self, request: Request):
        auth = await super().__call__(request)
        data = validate_token(auth.credentials)

        if data['email'] != "admin@gmail.com":
            raise HTTPException(
                status_code=403,
                detail="Credenciales incorrectas"
            )
```

Esta dependencia realiza el siguiente proceso:

1. `HTTPBearer` obtiene el encabezado `Authorization`.
2. Extrae el token que aparece después de `Bearer`.
3. `validate_token()` verifica la firma y decodifica el JWT.
4. Se obtiene el campo `email` del payload.
5. Se permite el acceso solamente al email `admin@gmail.com`.

## 6. Protección de endpoints

FastAPI protege un endpoint agregando la dependencia:

```python
dependencies=[Depends(JWTBearerToken())]
```

En `app/api/routes.py`, actualmente se usa en los siguientes endpoints.

### Listar pedidos

```python
@router.get(
    "/pedidos",
    response_model=list[PedidoResponse],
    dependencies=[Depends(JWTBearerToken())]
)
def get_all():
    return pedido_service.get_all()
```

### Crear un pedido

```python
@router.post(
    "/pedidos",
    response_model=PedidoResponse,
    dependencies=[Depends(JWTBearerToken())]
)
def create(pedido: PedidoCreate):
    return pedido_service.create(pedido.dict())
```

## Estado de protección actual

| Método | Endpoint | Función | JWT |
|---|---|---|---|
| `POST` | `/login` | Generar el token | No requiere token |
| `GET` | `/pedidos` | Listar pedidos | Protegido |
| `POST` | `/pedidos` | Crear un pedido | Protegido |
| `GET` | `/pedidos/{id}` | Consultar un pedido | No protegido |
| `PUT` | `/pedidos/{id}` | Actualizar un pedido | No protegido |
| `DELETE` | `/pedidos/{id}` | Eliminar un pedido | No protegido |

La protección no está aplicada uniformemente a todo el CRUD. Para proteger los endpoints restantes se puede agregar la misma dependencia a cada ruta o configurarla en el `APIRouter`.

Ejemplo para proteger todas las rutas registradas en un router:

```python
router = APIRouter(
    dependencies=[Depends(JWTBearerToken())]
)
```

El endpoint `/login` debe permanecer fuera de ese router protegido para que el cliente pueda obtener su primer token.

## Respuestas esperadas

| Situación | Respuesta recomendada |
|---|---|
| Credenciales correctas | `200 OK` con el token |
| Credenciales incorrectas | `401 Unauthorized` |
| Encabezado Bearer ausente | `401 Unauthorized` o `403 Forbidden`, según configuración |
| Token inválido o modificado | `401 Unauthorized` |
| Token expirado | `401 Unauthorized` |
| Usuario sin autorización | `403 Forbidden` |

## Consideraciones de seguridad actuales

La implementación demuestra el flujo básico de JWT, pero antes de usarla en producción deben atenderse los siguientes puntos:

- Las credenciales están escritas directamente en `main_api.py`.
- El secreto JWT está escrito directamente en `jwt_manager.py`.
- El payload incluye la contraseña del usuario.
- El token no contiene una fecha de expiración `exp`.
- No se validan claims como `sub`, `iss` o `aud`.
- No se controla explícitamente la excepción producida por un token inválido.
- Solamente dos endpoints de pedidos están protegidos.
- En la rama de acceso denegado, el código actual intenta ejecutar `.decode()` sobre `HTTPException`; esa llamada debe eliminarse.

## Configuración recomendada

El secreto y las credenciales deben mantenerse fuera del código mediante variables de entorno:

```text
JWT_SECRET=<secreto-largo-y-aleatorio>
JWT_ALGORITHM=HS256
JWT_EXPIRATION_MINUTES=30
```

El payload recomendado debe contener sólo identificadores y claims de seguridad:

```json
{
  "sub": "admin@gmail.com",
  "role": "admin",
  "iat": 1784552400,
  "exp": 1784554200
}
```

Nunca se debe incluir la contraseña en el JWT.

## Resumen

```text
POST /login
    → Validar credenciales
    → create_token()
    → Firmar JWT con HS256
    → Devolver token

Endpoint protegido
    → Leer Authorization: Bearer
    → JWTBearerToken
    → validate_token()
    → Verificar firma y usuario
    → Permitir o rechazar la operación
```

