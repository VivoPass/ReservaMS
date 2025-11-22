using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ReservasService.Aplicacion.DTOS;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId
{
    public record ObtenerReservasIdQuery(Guid ReservaGuid)
        : IRequest<ReservaUsuarioDTO?>;

}
