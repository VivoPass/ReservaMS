using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Commands.Reservas.CancelarReserva
{
    public record CancelarReservaCommand(Guid ReservaId) : IRequest<bool>;
}
