using ReservasService.Dominio.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReservasService.Dominio.Entidades
{
    public class Reserva
    {
        public Guid Id { get; private set; }

        public Id EventId { get; private set; } = null!;
        public Id ZonaEventoId { get; private set; } = null!;
        public Id AsientoId { get; private set; } = null!;     // asiento principal (primero)
        public Id UsuarioId { get; private set; } = null!;

        public ReservaEstado Estado { get; private set; }

        public DateTime CreadaEn { get; private set; }
        public DateTime? ExpiraEn { get; private set; }

        public decimal PrecioTotal { get; private set; }

        private readonly List<ReservaAsiento> _asientos = new();
        public IReadOnlyCollection<ReservaAsiento> Asientos => _asientos.AsReadOnly();

        private Reserva() { }

        public enum ReservaEstado
        {
            Hold,
            Confirmada,
            Liberada,
            Expirada
        }

        // -----------------------------
        //   CREACIÓN MULTI-ASIENTO
        // -----------------------------
        public static Reserva CrearHold(
            Id eventId,
            Id zonaEventoId,
            Id usuarioId,
            IEnumerable<(Id asientoId, decimal precioUnitario, string label)> asientos,
            TimeSpan tiempoHold)
        {
            if (asientos == null || !asientos.Any())
                throw new ArgumentException("Debe haber al menos un asiento.");

            var lista = asientos.ToList();

            var reserva = new Reserva
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ZonaEventoId = zonaEventoId,
                UsuarioId = usuarioId,
                Estado = ReservaEstado.Hold,
                CreadaEn = DateTime.UtcNow,
                ExpiraEn = DateTime.UtcNow.Add(tiempoHold)
            };

            // asiento principal = el primero
            reserva.AsientoId = lista.First().asientoId;

            foreach (var a in lista)
            {
                reserva._asientos.Add(new ReservaAsiento(
                    Guid.NewGuid(),
                    a.asientoId,
                    a.precioUnitario,
                    a.label
                ));
            }

            reserva.PrecioTotal = reserva._asientos.Sum(x => x.PrecioUnitario);

            return reserva;
        }

        // -----------------------------
        //  REHIDRATAR DESDE MONGO
        // -----------------------------
        public static Reserva Rehidratar(
            Guid id,
            Id eventId,
            Id zonaEventoId,
            Id asientoId,
            Id usuarioId,
            ReservaEstado estado,
            DateTime creadaEn,
            DateTime? expiraEn,
            decimal precioTotal
        )
        {
            return new Reserva
            {
                Id = id,
                EventId = eventId,
                ZonaEventoId = zonaEventoId,
                AsientoId = asientoId,
                UsuarioId = usuarioId,
                Estado = estado,
                CreadaEn = creadaEn,
                ExpiraEn = expiraEn,
                PrecioTotal = precioTotal
            };
        }

        // Agregar asientos reconstruidos desde Infraestructura
        public void AgregarAsientoDesdeDocumento(Guid id, Id asientoId, decimal precioUnitario, string label)
        {
            _asientos.Add(new ReservaAsiento(id, asientoId, precioUnitario, label));
        }

        // -----------------------------
        //         ESTADOS
        // -----------------------------
        public void Confirmar()
        {
            if (Estado != ReservaEstado.Hold)
                throw new InvalidOperationException("Solo se pueden confirmar reservas en estado Hold.");

            if (ExpiraEn.HasValue && ExpiraEn.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("No se puede confirmar una reserva expirada.");

            Estado = ReservaEstado.Confirmada;
        }

        public void LiberarPorCancelacion()
        {
            if (Estado != ReservaEstado.Hold)
                throw new InvalidOperationException("Solo se pueden liberar reservas en estado Hold.");

            Estado = ReservaEstado.Liberada;
        }

        public void MarcarComoExpirada()
        {
            if (Estado != ReservaEstado.Hold)
                return;

            Estado = ReservaEstado.Expirada;
        }

        public bool EstaExpirada()
        {
            return ExpiraEn.HasValue && ExpiraEn.Value <= DateTime.UtcNow;
        }
    }
}
