
// ReservasService.Infraestructura/Reservas/ReservaRepository.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;

namespace ReservasService.Infraestructura.Repositorios
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly IMongoCollection<ReservaDocument> _collection;

        public ReservaRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<ReservaDocument>("Reservas");
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

        // ------------------ MAPEOS ------------------

        private static ReservaDocument ToDocument(Reserva reserva)
        {
            return new ReservaDocument
            {
                Id = reserva.Id,
                EventId = reserva.EventId.Value,
                ZonaEventoId = reserva.ZonaEventoId.Value,
                AsientoId = reserva.AsientoId.Value,
                UsuarioId = reserva.UsuarioId.Value,
                Estado = (int)reserva.Estado,
                CreadaEn = reserva.CreadaEn,
                ExpiraEn = reserva.ExpiraEn
            };
        }

        private static Reserva ToDomain(ReservaDocument doc)
        {
            var eventId = new Id(doc.EventId, "EventId");
            var zonaEventoId = new Id(doc.ZonaEventoId, "ZonaEventoId");
            var asientoId = new Id(doc.AsientoId, "AsientoId");
            var usuarioId = new Id(doc.UsuarioId, "UsuarioId");

            var estado = (Reserva.ReservaEstado)doc.Estado;

            return Reserva.Rehidratar(
                doc.Id,
                eventId,
                zonaEventoId,
                asientoId,
                usuarioId,
                estado,
                doc.CreadaEn,
                doc.ExpiraEn
            );
        }
    }
}
