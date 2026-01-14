using log4net;
using log4net.Config;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Reservas.Infrastructure.Configurations;
using Reservas.Infrastructure.Interfaces;
using Reservas.Infrastructure.Persistences.Repositories;
using ReservasService.Api.Controllers;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using ReservasService.Infraestructura.Configuracion;
using ReservasService.Infraestructura.Repositorios;
using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ---------------------- Services ----------------------
// Configurar log4net
XmlConfigurator.Configure(new FileInfo("log4net.config"));
builder.Services.AddSingleton<ILog>(provider => LogManager.GetLogger(typeof(ReservasController)));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReservasService API",
        Version = "v1",
        Description = "API del Microservicio de Reservas que gestiona la información de las reservas realizadas.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);

});

// MongoDB
builder.Services.AddMongoDb(builder.Configuration);

// MediatR v13
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CrearReservasPorZonaYCantidadCommand).Assembly);
});

// Auditorías de Reservas
builder.Services.AddSingleton<AuditoriaDbConfig>();
builder.Services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();

// Repositorio de Reservas
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();

builder.Services.AddHostedService<ReservasService.Infraestructura.BackgroundJobs.AutoCancelacionHoldsService>();

// HttpClient hacia el microservicio de Eventos
builder.Services.AddHttpClient<IAsientosDisponibilidadService, AsientosRepository>(client =>
{
    var baseUrl = builder.Configuration["EventsService:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Falta la configuración 'EventsService:BaseUrl' en appsettings.");

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("UsuariosClient", client =>
{
    var baseUrl = builder.Configuration["UsersService:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Falta la configuración 'UsersService:BaseUrl' en appsettings.");

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