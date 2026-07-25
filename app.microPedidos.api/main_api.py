from http.client import HTTPResponse

from fastapi.responses import HTMLResponse, JSONResponse
from fastapi import FastAPI
from app.api.routes import router
from app.core.database import engine, Base
from app.schemas.schemas import Usuario
from app.services.jwt_manager import create_token

"""config para token"""



app = FastAPI()
app.title = "Api service MicroPedidos"
app.version = "0.0.1"

# Code First
Base.metadata.create_all(bind=engine)

app.include_router(router)


"""Api para obtener el Token"""
@app.post('/login', tags=['seguridad'])
def login(user: Usuario):
    if user.email == "admin@gmail.com" and user.password == "admin":
        token: str = create_token(user.model_dump())
        return JSONResponse(status_code=200, content=token)
    

@app.get("/")
def root():
    return {"message": "API MicroPedidos funcionando"}


@app.get('/', tags=['test'])
def message():
    return HTTPResponse('<h1>Aplicaciones Distribuidas -- Apis en Python</h1>')