using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Reserva
{
    public class NoHayAsientosSuficientesException : Exception
    {
        public NoHayAsientosSuficientesException(Guid eventId, Guid zonaId, int requeridos, int disponibles)
            : base($"No hay asientos suficientes en el evento '{eventId}', zona '{zonaId}'. Requeridos={requeridos}, Disponibles={disponibles}.")
        {
        }
    }

    public class CrearReservasPorZonaYCantidadCommandHandlerException : Exception
    {
        public CrearReservasPorZonaYCantidadCommandHandlerException(Exception inner)
            : base("Error inesperado en CrearReservasPorZonaYCantidadCommandHandler.", inner)
        {
        }
    }
}
