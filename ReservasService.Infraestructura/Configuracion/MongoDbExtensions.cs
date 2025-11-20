using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ReservasService.Infraestructura.Configuracion
{
    public static class MongoDbExtensions
    {
        public static IServiceCollection AddMongoDb(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Leer la sección "MongoDb" manualmente
            var section = configuration.GetSection("MongoDb");

            var settings = new MongoDbSettings
            {
                ConnectionString = section["ConnectionString"]
                                   ?? throw new InvalidOperationException("MongoDb:ConnectionString no está configurado."),
                DatabaseName = section["DatabaseName"]
                               ?? throw new InvalidOperationException("MongoDb:DatabaseName no está configurado.")
            };

            // Registrar MongoClient
            services.AddSingleton<IMongoClient>(_ => new MongoClient(settings.ConnectionString));

            // Registrar IMongoDatabase
            services.AddScoped<IMongoDatabase>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(settings.DatabaseName);
            });

            return services;
        }
    }
}