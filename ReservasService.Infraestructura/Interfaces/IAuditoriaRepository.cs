using System.Threading.Tasks;

namespace Reservas.Infrastructure.Interfaces
{
    /// <summary>
    /// Define las operaciones de auditoría para el microservicio de Reservas.
    /// </summary>
    public interface IAuditoriaRepository
    {
        /// <summary>
        /// Inserta una auditoría asociada a una reserva (creación, modificación, cancelación, etc.).
        /// </summary>
        /// <param name="idReserva">ID de la reserva afectada.</param>
        /// <param name="level">Nivel del log (INFO, WARN, ERROR).</param>
        /// <param name="tipo">Clasificación del evento de auditoría.</param>
        /// <param name="mensaje">Mensaje descriptivo.</param>
        Task InsertarAuditoriaReserva(string idReserva, string level, string tipo, string mensaje);

        /// <summary>
        /// Inserta una auditoría genérica de operación relacionada con el flujo de reservas.
        /// </summary>
        /// <param name="idReferencia">ID de referencia (reserva, evento, usuario, etc.).</param>
        /// <param name="level">Nivel del log (INFO, WARN, ERROR).</param>
        /// <param name="tipo">Clasificación del evento de auditoría.</param>
        /// <param name="mensaje">Mensaje descriptivo.</param>
        Task InsertarAuditoriaOperacion(string idReferencia, string level, string tipo, string mensaje);
    }
}