using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Interfacess
{
    public interface IAsientosDisponibilidadService
    {
        /// <summary>
        /// Devuelve una lista de IDs de asientos disponibles en la zona para el evento.
        /// </summary>
        Task<List<Guid>> ObtenerAsientosDisponiblesAsync(
            Guid eventId,
            Guid zonaEventoId,
            int cantidad,
            CancellationToken ct = default);

        Task ActualizarEstadoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            string nuevoEstado,
            CancellationToken ct = default);
    }
}
