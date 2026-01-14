using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Commands.Reservas.CancelacionAutomatica
{
    public record CancelarHoldsExpiradosCommand : IRequest;
}
