using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ReservasService.Aplicacion.DTOS;

namespace ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona
{
    public class CrearReservasPorZonaYCantidadCommand : IRequest<List<ReservaHoldResultDTO>>
    {
        public Guid EventId { get; init; }
        public Guid ZonaEventoId { get; init; }
        public int CantidadBoletos { get; init; }
        public Guid UsuarioId { get; init; }
        public TimeSpan TiempoHold { get; init; } = TimeSpan.FromMinutes(10);
    }
}
