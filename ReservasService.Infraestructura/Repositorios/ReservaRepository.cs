// ReservasService.Infraestructura/Reservas/ReservaRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using log4net;

using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;
using ReservasService.Dominio.Excepciones.Infraestructura;
using Reservas.Infrastructure.Interfaces;   // IAuditoriaRepository

namespace ReservasService.Infraestructura.Repositorios
{
    /// <summary>
    /// Proporciona la capa de acceso a datos para la entidad Reserva sobre MongoDB.
    /// </summary>
    public class ReservaRepository : IReservaRepository
    {
        private readonly IMongoCollection<ReservaDocument> _collection;
        private readonly ILog _logger;
        private readonly IAuditoriaRepository _auditoriaRepository;

        public ReservaRepository(
            IMongoDatabase database,
            ILog logger,
            IAuditoriaRepository auditoriaRepository)
        {
            _collection = database.GetCollection<ReservaDocument>("reservas");
            _logger = logger ?? throw new LoggerNullException();
            _auditoriaRepository = auditoriaRepository ?? throw new AuditoriaRepositoryNullException();
        }

        #region ExisteHoldActivoAsync
        public async Task<bool> ExisteHoldActivoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            CancellationToken ct = default)
        {
            _logger.Debug($"[Reservas] Verificando hold activo. EventId='{eventId}', ZonaEventoId='{zonaEventoId}', AsientoId='{asientoId}'.");

            try
            {
                var builder = Builders<ReservaDocument>.Filter;

                var filter = builder.Eq(x => x.EventId, eventId) &
                             builder.Eq(x => x.ZonaEventoId, zonaEventoId) &
                             builder.Eq(x => x.AsientoId, asientoId) &
                             builder.Eq(x => x.Estado, (int)Reserva.ReservaEstado.Hold) &
                             builder.Gt(x => x.ExpiraEn, DateTime.UtcNow);

                var count = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);

                _logger.Debug($"[Reservas] Hold activo encontrado: {(count > 0)}.");
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.Error("[Reservas] Error al verificar hold activo.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        #region CrearAsync
        public async Task CrearAsync(Reserva reserva, CancellationToken ct = default)
        {
            _logger.Info($"[Reservas] Iniciando creación de reserva ID='{reserva.Id}' para Usuario='{reserva.UsuarioId.Value}'.");

            try
            {
                var doc = ToDocument(reserva);
                await _collection.InsertOneAsync(doc, cancellationToken: ct);

                _logger.Info($"[Reservas] Reserva ID='{reserva.Id}' insertada exitosamente en MongoDB.");

                await _auditoriaRepository.InsertarAuditoriaReserva(
                    reserva.Id.ToString(),
                    "INFO",
                    "RESERVA_CREADA",
                    $"Se creó la reserva '{reserva.Id}' para el usuario '{reserva.UsuarioId.Value}' en el evento '{reserva.EventId.Value}'."
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"[Reservas] Error al crear reserva ID='{reserva.Id}'.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        #region ObtenerPorIdAsync
        public async Task<Reserva?> ObtenerPorIdAsync(Guid reservaId, CancellationToken ct = default)
        {
            _logger.Debug($"[Reservas] Buscando reserva por ID='{reservaId}'.");

            try
            {
                var filter = Builders<ReservaDocument>.Filter.Eq(x => x.Id, reservaId);
                var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);

                if (doc is null)
                {
                    _logger.Info($"[Reservas] No se encontró reserva con ID='{reservaId}'.");
                    return null;
                }

                var reserva = ToDomain(doc);
                _logger.Debug($"[Reservas] Reserva ID='{reservaId}' encontrada y mapeada a dominio.");
                return reserva;
            }
            catch (Exception ex)
            {
                _logger.Error($"[Reservas] Error al obtener reserva por ID='{reservaId}'.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        #region ActualizarAsync
        public async Task ActualizarAsync(Reserva reserva, CancellationToken ct = default)
        {
            _logger.Info($"[Reservas] Iniciando actualización de reserva ID='{reserva.Id}'.");

            try
            {
                var doc = ToDocument(reserva);
                var filter = Builders<ReservaDocument>.Filter.Eq(x => x.Id, doc.Id);

                var result = await _collection.ReplaceOneAsync(filter, doc, cancellationToken: ct);

                if (result.ModifiedCount == 0)
                {
                    _logger.Warn($"[Reservas] Actualización de reserva ID='{reserva.Id}' no modificó ningún documento.");
                    return;
                }

                _logger.Info($"[Reservas] Reserva ID='{reserva.Id}' actualizada. Documentos modificados: {result.ModifiedCount}.");

                await _auditoriaRepository.InsertarAuditoriaReserva(
                    reserva.Id.ToString(),
                    "INFO",
                    "RESERVA_ACTUALIZADA",
                    $"Se actualizó la reserva '{reserva.Id}' con estado '{reserva.Estado}' y precio total '{reserva.PrecioTotal}'."
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"[Reservas] Error al actualizar reserva ID='{reserva.Id}'.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        #region ObtenerHoldsExpiradosHastaAsync
        public async Task<List<Reserva>> ObtenerHoldsExpiradosHastaAsync(
            DateTime fechaLimite,
            CancellationToken ct = default)
        {
            _logger.Debug($"[Reservas] Buscando holds expirados hasta '{fechaLimite:O}'.");

            try
            {
                var builder = Builders<ReservaDocument>.Filter;

                var filter = builder.Eq(x => x.Estado, (int)Reserva.ReservaEstado.Hold) &
                             builder.Lt(x => x.ExpiraEn, fechaLimite);

                var docs = await _collection.Find(filter).ToListAsync(ct);

                var result = new List<Reserva>();
                foreach (var doc in docs)
                    result.Add(ToDomain(doc));

                _logger.Debug($"[Reservas] Encontradas {result.Count} reservas con hold expirado.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("[Reservas] Error al obtener holds expirados.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        #region ObtenerPorUsuarioAsync
        public async Task<List<Reserva>> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
        {
            _logger.Debug($"[Reservas] Buscando reservas para UsuarioId='{usuarioId}'.");

            try
            {
                var builder = Builders<ReservaDocument>.Filter;
                var filter = builder.Eq(x => x.UsuarioId, usuarioId);

                var docs = await _collection.Find(filter).ToListAsync(ct);

                var result = new List<Reserva>();
                foreach (var doc in docs)
                    result.Add(ToDomain(doc));

                _logger.Debug($"[Reservas] Encontradas {result.Count} reservas para UsuarioId='{usuarioId}'.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"[Reservas] Error al obtener reservas por UsuarioId='{usuarioId}'.", ex);
                throw new ReservaRepositoryException(ex);
            }
        }
        #endregion

        // ------------------ MAPEOS ------------------

        private static ReservaDocument ToDocument(Reserva reserva)
        {
            return new ReservaDocument
            {
                Id = reserva.Id,
                EventId = reserva.EventId.Value,
                ZonaEventoId = reserva.ZonaEventoId.Value,
                AsientoId = reserva.AsientoId?.Value ?? Guid.Empty,   // por compatibilidad
                UsuarioId = reserva.UsuarioId.Value,
                Estado = (int)reserva.Estado,
                CreadaEn = reserva.CreadaEn,
                ExpiraEn = reserva.ExpiraEn,
                PrecioTotal = reserva.PrecioTotal,
                Asientos = reserva.Asientos
                    .Select(a => new ReservaAsientoDocument
                    {
                        Id = a.Id,
                        AsientoId = a.AsientoId.Value,
                        PrecioUnitario = a.PrecioUnitario,
                        Label = a.Label
                    })
                    .ToList()
            };
        }

        private static Reserva ToDomain(ReservaDocument doc)
        {
            var reserva = Reserva.Rehidratar(
                doc.Id,
                new Id(doc.EventId, "EventId"),
                new Id(doc.ZonaEventoId, "ZonaEventoId"),
                new Id(doc.AsientoId, "AsientoId"),
                new Id(doc.UsuarioId, "UsuarioId"),
                (Reserva.ReservaEstado)doc.Estado,
                doc.CreadaEn,
                doc.ExpiraEn,
                doc.PrecioTotal
            );

            if (doc.Asientos != null)
            {
                foreach (var a in doc.Asientos)
                {
                    reserva.AgregarAsientoDesdeDocumento(
                        a.Id,
                        new Id(a.AsientoId, "AsientoId"),
                        a.PrecioUnitario,
                        a.Label
                    );
                }
            }

            return reserva;
        }
    }
}
