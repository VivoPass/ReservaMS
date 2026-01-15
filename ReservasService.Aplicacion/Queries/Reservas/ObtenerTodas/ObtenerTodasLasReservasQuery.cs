using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReservasService.Aplicacion.DTOS;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerTodas
{
    public class ObtenerTodasLasReservasQuery : IRequest<List<ReservaUsuarioDTO>>
    {
    }
}
