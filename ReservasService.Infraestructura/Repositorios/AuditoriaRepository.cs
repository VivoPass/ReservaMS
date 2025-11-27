using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Reservas.Infrastructure.Configurations;
using Reservas.Infrastructure.Interfaces;
using ReservasService.Dominio.Excepciones.Infraestructura;

namespace Reservas.Infrastructure.Persistences.Repositories
{
    /// <summary>
    /// Repositorio encargado de la gestión de la colección de Auditorías en MongoDB
    /// para el microservicio de Reservas.
    /// </summary>
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly IMongoCollection<BsonDocument> AuditoriaColeccion;
        private readonly ILog Log;

        public AuditoriaRepository(AuditoriaDbConfig mongoConfig, ILog log)
        {
            AuditoriaColeccion = mongoConfig.db.GetCollection<BsonDocument>("auditoriaReservas");
            Log = log ?? throw new LoggerNullException();
        }

        #region InsertarAuditoriaReserva(string idReserva, string level, string tipo, string mensaje)
        /// <summary>
        /// Inserta un registro de auditoría asociado a una Reserva.
        /// </summary>
        public async Task InsertarAuditoriaReserva(string idReserva, string level, string tipo, string mensaje)
        {
            try
            {
                var documento = new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "idReserva", idReserva },
                    { "level", level },
                    { "tipo", tipo },
                    { "mensaje", mensaje },
                    { "timestamp", DateTime.UtcNow }
                };

                await AuditoriaColeccion.InsertOneAsync(documento);
                Log.Debug($"Auditoría de Reserva insertada: Tipo='{tipo}', ID Reserva='{idReserva}'.");
            }
            catch (MongoException ex)
            {
                Log.Error($"[FATAL DB ERROR] Fallo al insertar auditoría de reserva (ID Reserva: {idReserva}). Detalles: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Fatal($"[FATAL ERROR] Excepción no controlada al insertar auditoría de reserva (ID Reserva: {idReserva}).", ex);
                throw new AuditoriaRepositoryException(ex);
            }
        }
        #endregion

        #region InsertarAuditoriaOperacion(string idReferencia, string level, string tipo, string mensaje)
        /// <summary>
        /// Inserta un registro de auditoría genérico asociado a una operación relacionada con Reservas.
        /// </summary>
        public async Task InsertarAuditoriaOperacion(string idReferencia, string level, string tipo, string mensaje)
        {
            try
            {
                var documento = new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "idReferencia", idReferencia },
                    { "level", level },
                    { "tipo", tipo },
                    { "mensaje", mensaje },
                    { "timestamp", DateTime.UtcNow }
                };

                await AuditoriaColeccion.InsertOneAsync(documento);
                Log.Debug($"Auditoría de operación insertada: Tipo='{tipo}', ID Referencia='{idReferencia}'.");
            }
            catch (MongoException ex)
            {
                Log.Error($"[FATAL DB ERROR] Fallo al insertar auditoría de operación (ID Ref: {idReferencia}). Detalles: {ex.Message}", ex);
                throw;
            }
            catch (Exception ex)
            {
                Log.Fatal($"[FATAL ERROR] Excepción no controlada al insertar auditoría de operación (ID Ref: {idReferencia}).", ex);
                throw new AuditoriaRepositoryException(ex);
            }
        }
        #endregion
    }
}
