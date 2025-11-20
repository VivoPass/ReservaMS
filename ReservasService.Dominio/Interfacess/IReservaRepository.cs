using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReservasService.Dominio.Entidades;

namespace ReservasService.Dominio.Interfaces
{
    public interface IReservaRepository
    {
        /// <summary>
        /// Verifica si existe un hold activo para un asiento específico.
        /// </summary>
        Task<bool> ExisteHoldActivoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            CancellationToken ct = default);

        /// <summary>
        /// Guarda una nueva reserva.
        /// </summary>
        Task CrearAsync(Reserva reserva, CancellationToken ct = default);

        /// <summary>
        /// Obtiene una reserva por su ID.
        /// </summary>
        Task<Reserva?> ObtenerPorIdAsync(Guid reservaId, CancellationToken ct = default);

        /// <summary>
        /// Actualiza una reserva existente.
        /// </summary>
        Task ActualizarAsync(Reserva reserva, CancellationToken ct = default);

        /// <summary>
        /// Obtiene todos los holds que ya expiraron.
        /// </summary>
        Task<List<Reserva>> ObtenerHoldsExpiradosHastaAsync(
            DateTime fechaLimite,
            CancellationToken ct = default);
    }
}