using app.microCliente.common.EventMQ;
using app.microCliente.dataAccess.context;
using app.microCliente.dataAccess.repositories;
using app.microCliente.services.EventMQ;
using app.microCliente.services.Implementations;
using app.microCliente.services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Servicios MVC
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cadena de conexión SQL Server
var conSqlServer = builder.Configuration.GetConnectionString("BDDSqlServer");

if (string.IsNullOrEmpty(conSqlServer))
{
    throw new Exception("No se encontró la cadena de conexión BDDSqlServer");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(conSqlServer);
    options.LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging();
});

// Configuración RabbitMQ
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection("rabbitmq")
);

// Repositorios y servicios
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();

builder.Services.AddScoped<IDireccionClienteRepository, DireccionClienteRepository>();
builder.Services.AddScoped<IDireccionClienteService, DireccionClienteService>();

builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

var app = builder.Build();

// Swagger solamente en desarrollo
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS solo fuera de Docker
if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Ejecutar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();