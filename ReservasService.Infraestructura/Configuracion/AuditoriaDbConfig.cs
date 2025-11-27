using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using ReservasService.Dominio.Excepciones.Infraestructura;

namespace Reservas.Infrastructure.Configurations
{
    public class AuditoriaDbConfig
    {
        public MongoClient client;
        public IMongoDatabase db;

        public AuditoriaDbConfig(IConfiguration configuration)
        {
            try
            {
                // Lee la cadena de conexión desde appsettings: "MongoDb:ConnectionString"
                string connectionUri = configuration["MongoDb:ConnectionString"];
                if (string.IsNullOrWhiteSpace(connectionUri))
                    throw new ConexionBdInvalida();

                // Lee el nombre de la BD de auditorías: "MongoDb:AuditoriaDatabaseName"
                string databaseName = configuration["MongoDb:AuditoriaDatabaseName"];
                if (string.IsNullOrWhiteSpace(databaseName))
                    throw new NombreBdInvalido();

                var settings = MongoClientSettings.FromConnectionString(connectionUri);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);

                client = new MongoClient(settings);
                db = client.GetDatabase(databaseName);
            }
            catch (MongoException ex)
            {
                throw new MongoDBConnectionException(ex);
            }
            catch (Exception ex)
            {
                throw new MongoDBUnnexpectedException(ex);
            }
        }
    }
}