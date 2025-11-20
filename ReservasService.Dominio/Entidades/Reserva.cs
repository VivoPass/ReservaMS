using ReservasService.Dominio.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Entidades
{
    public class Reserva
    {
        public Guid Id { get; private set; }

        public Id EventId { get; private set; } = null!;
        public Id ZonaEventoId { get; private set; } = null!;
        public Id AsientoId { get; private set; } = null!;
        public Id UsuarioId { get; private set; } = null!;

        public ReservaEstado Estado { get; private set; }

        public DateTime CreadaEn { get; private set; }
        public DateTime? ExpiraEn { get; private set; }

        private Reserva() { } 

        public enum ReservaEstado
        {
            Hold,
            Confirmada,
            Liberada,
            Expirada
        }

        private Reserva(
            Id eventId,
            Id zonaEventoId,
            Id asientoId,
            Id usuarioId,
            TimeSpan duracionHold)
        {
            if (duracionHold <= TimeSpan.Zero)
                throw new ArgumentException("La duración del hold debe ser mayor que cero.", nameof(duracionHold));

            Id = Guid.NewGuid();

            EventId = eventId ?? throw new ArgumentNullException(nameof(eventId));
            ZonaEventoId = zonaEventoId ?? throw new ArgumentNullException(nameof(zonaEventoId));
            AsientoId = asientoId ?? throw new ArgumentNullException(nameof(asientoId));
            UsuarioId = usuarioId ?? throw new ArgumentNullException(nameof(usuarioId));

            Estado = ReservaEstado.Hold;

            CreadaEn = DateTime.UtcNow;
            ExpiraEn = CreadaEn.Add(duracionHold);
        }

        public static Reserva CrearHold(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            Guid usuarioId,
            TimeSpan duracionHold)
        {
            return new Reserva(
                new Id(eventId, "EventId"),
                new Id(zonaEventoId, "ZonaEventoId"),
                new Id(asientoId, "AsientoId"),
                new Id(usuarioId, "UsuarioId"),
                duracionHold
            );
        }

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

        public static Reserva Rehidratar(
            Guid id,
            Id eventId,
            Id zonaEventoId,
            Id asientoId,
            Id usuarioId,
            ReservaEstado estado,
            DateTime creadaEn,
            DateTime? expiraEn)
        {
            var reserva = new Reserva();

            reserva.Id = id;
            reserva.EventId = eventId;
            reserva.ZonaEventoId = zonaEventoId;
            reserva.AsientoId = asientoId;
            reserva.UsuarioId = usuarioId;
            reserva.Estado = estado;
            reserva.CreadaEn = creadaEn;
            reserva.ExpiraEn = expiraEn;

            return reserva;
        }
    }
}
