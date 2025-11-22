using MediatR;
using ReservasService.Aplicacion.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario
{
    public record ObtenerReservasPorUsuarioQuery(Guid UsuarioId)
        : IRequest<List<ReservaUsuarioDTO>>;
}
