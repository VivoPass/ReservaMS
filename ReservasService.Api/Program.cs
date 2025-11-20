using System;
using MediatR;
using Microsoft.OpenApi;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using ReservasService.Infraestructura.Configuracion;
using ReservasService.Infraestructura.Repositorios;

var builder = WebApplication.CreateBuilder(args);

// ---------------------- Services ----------------------

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReservasService API",
        Version = "v1"
    });
});

// MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// MediatR v13
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CrearReservasPorZonaYCantidadCommand).Assembly);
});

// Repositorio de Reservas
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();

// HttpClient hacia el microservicio de Eventos
builder.Services.AddHttpClient<IAsientosDisponibilidadService, AsientosRepository>(client =>
{
    var baseUrl = builder.Configuration["EventsService:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Falta la configuración 'EventsService:BaseUrl' en appsettings.");

    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// ---------------------- Pipeline ----------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();