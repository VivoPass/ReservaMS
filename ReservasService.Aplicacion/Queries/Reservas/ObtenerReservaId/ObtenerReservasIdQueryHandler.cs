using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario;
using ReservasService.Dominio.Interfaces;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId
{

    public class ObtenerReservasIdQueryHandler
        : IRequestHandler<ObtenerReservasIdQuery, ReservaUsuarioDTO?>
    {
        private readonly IReservaRepository _reservaRepository;

        public ObtenerReservasIdQueryHandler(IReservaRepository reservaRepository)
        {
            _reservaRepository = reservaRepository;
        }

        public async Task<ReservaUsuarioDTO?> Handle(
            ObtenerReservasIdQuery request,
            CancellationToken cancellationToken)
        {
            var r = await _reservaRepository.ObtenerPorIdAsync(
                request.ReservaGuid,
                cancellationToken);

            if (r is null)
                return null; // o lanzar excepción si prefieres

            var dto = new ReservaUsuarioDTO
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
            };

            return dto;
        }
    }
}
