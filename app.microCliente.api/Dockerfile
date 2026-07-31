# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar TODOS los proyectos
COPY ["app.microCliente.api/app.microCliente.api.csproj", "app.microCliente.api/"]
COPY ["app.microCliente.common/app.microCliente.common.csproj", "app.microCliente.common/"]
COPY ["app.microCliente.dataAccess/app.microCliente.dataAccess.csproj", "app.microCliente.dataAccess/"]
COPY ["app.microCliente.entities/app.microCliente.entities.csproj", "app.microCliente.entities/"]

# Restaurar
RUN dotnet restore "app.microCliente.api/app.microCliente.api.csproj"

# Copiar todo el código
COPY . .

# Publicar
WORKDIR "/src/app.microCliente.api"
RUN dotnet publish "app.microCliente.api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "app.microCliente.api.dll"]