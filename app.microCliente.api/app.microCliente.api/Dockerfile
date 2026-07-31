# Etapa 1: imagen para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["app.microCliente.api.csproj", "./"]

RUN dotnet restore "app.microCliente.api.csproj"

COPY . .

RUN dotnet publish "app.microCliente.api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# Etapa 2: imagen final para ejecutar la API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "app.microCliente.api.dll"]