
// ReservasService.Infraestructura/Reservas/ReservaRepository.cs
using MongoDB.Driver;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using static System.Net.Mime.MediaTypeNames;

namespace ReservasService.Infraestructura.Repositorios
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly IMongoCollection<ReservaDocument> _collection;
        private readonly ILog _logger;

        public ReservaRepository(IMongoDatabase database, ILog logger)
        {
            _collection = database.GetCollection<ReservaDocument>("reservas");
            _logger = logger;
        }

        public async Task<bool> ExisteHoldActivoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            CancellationToken ct = default)
        {
            var builder = Builders<ReservaDocument>.Filter;

            var filter = builder.Eq(x => x.EventId, eventId) &
                         builder.Eq(x => x.ZonaEventoId, zonaEventoId) &
                         builder.Eq(x => x.AsientoId, asientoId) &
                         builder.Eq(x => x.Estado, (int)Reserva.ReservaEstado.Hold) &
                         builder.Gt(x => x.ExpiraEn, DateTime.UtcNow);

            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
            return count > 0;
        }

        public async Task CrearAsync(Reserva reserva, CancellationToken ct = default)
        {
            var doc = ToDocument(reserva);
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
        }

        public async Task<Reserva?> ObtenerPorIdAsync(Guid reservaId, CancellationToken ct = default)
        {
            var filter = Builders<ReservaDocument>.Filter.Eq(x => x.Id, reservaId);
            var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);

            return doc is null ? null : ToDomain(doc);
        }

        public async Task ActualizarAsync(Reserva reserva, CancellationToken ct = default)
        {
            var doc = ToDocument(reserva);
            var filter = Builders<ReservaDocument>.Filter.Eq(x => x.Id, doc.Id);

            await _collection.ReplaceOneAsync(filter, doc, cancellationToken: ct);
        }

        public async Task<List<Reserva>> ObtenerHoldsExpiradosHastaAsync(
            DateTime fechaLimite,
            CancellationToken ct = default)
        {
            var builder = Builders<ReservaDocument>.Filter;

            var filter = builder.Eq(x => x.Estado, (int)Reserva.ReservaEstado.Hold) &
                         builder.Lt(x => x.ExpiraEn, fechaLimite);

            var docs = await _collection.Find(filter).ToListAsync(ct);

            var result = new List<Reserva>();
            foreach (var doc in docs)
                result.Add(ToDomain(doc));

            return result;
        }

        public async Task<List<Reserva>> ObtenerPorUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
        {
            var builder = Builders<ReservaDocument>.Filter;
            var filter = builder.Eq(x => x.UsuarioId, usuarioId);

            var docs = await _collection.Find(filter).ToListAsync(ct);

            var result = new List<Reserva>();
            foreach (var doc in docs)
                result.Add(ToDomain(doc));

            return result;
        }

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
                PrecioTotal = reserva.PrecioTotal,                    // 👈 NUEVO
                Asientos = reserva.Asientos                           // 👈 NUEVO
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
                doc.PrecioTotal              // 👈 NUEVO
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
