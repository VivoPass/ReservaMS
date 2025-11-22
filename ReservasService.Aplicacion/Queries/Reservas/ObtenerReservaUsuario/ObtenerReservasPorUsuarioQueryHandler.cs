using MediatR;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario
{
    public class ObtenerReservasPorUsuarioQueryHandler
        : IRequestHandler<ObtenerReservasPorUsuarioQuery, List<ReservaUsuarioDTO>>
    {
        private readonly IReservaRepository _reservaRepository;

        public ObtenerReservasPorUsuarioQueryHandler(IReservaRepository reservaRepository)
        {
            _reservaRepository = reservaRepository;
        }

        public async Task<List<ReservaUsuarioDTO>> Handle(
            ObtenerReservasPorUsuarioQuery request,
            CancellationToken cancellationToken)
        {
            var reservas = await _reservaRepository.ObtenerPorUsuarioAsync(
                request.UsuarioId,
                cancellationToken);

            var resultado = reservas
                .Select(r => new ReservaUsuarioDTO
                {
                    ReservaId = r.Id,
                    EventId = r.EventId.Value,
                    ZonaEventoId = r.ZonaEventoId.Value,
                    UsuarioId = r.UsuarioId.Value,
                    Estado = r.Estado.ToString(),
                    CreadaEn = r.CreadaEn,
                    ExpiraEn = r.ExpiraEn,
                    PrecioTotal = r.PrecioTotal,
                    Asientos = r.Asientos
                        .Select(a => new ReservaAsientoDTO
                        {
                            AsientoId = a.AsientoId.Value,
                            PrecioUnitario = a.PrecioUnitario,
                            Label = a.Label
                        })
                        .ToList()
                })
                .ToList();

            return resultado;
        }
    }
}
