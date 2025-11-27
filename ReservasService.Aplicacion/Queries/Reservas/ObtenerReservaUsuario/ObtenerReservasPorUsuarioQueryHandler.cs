using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Interfaces;

using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario
{
    public class ObtenerReservasPorUsuarioQueryHandler
        : IRequestHandler<ObtenerReservasPorUsuarioQuery, List<ReservaUsuarioDTO>>
    {
        private readonly IReservaRepository ReservaRepository;
        private readonly ILog Logger;

        public ObtenerReservasPorUsuarioQueryHandler(
            IReservaRepository reservaRepository,
            ILog logger)
        {
            ReservaRepository = reservaRepository ?? throw new ReservaRepositoryNullException();
            Logger = logger ?? throw new LoggerNullException();
        }

        public async Task<List<ReservaUsuarioDTO>> Handle(
            ObtenerReservasPorUsuarioQuery request,
            CancellationToken cancellationToken)
        {
            Logger.Debug($"[ObtenerReservasUsuario] Inicio. UsuarioId='{request.UsuarioId}'.");

            var reservas = await ReservaRepository.ObtenerPorUsuarioAsync(
                request.UsuarioId,
                cancellationToken);

            if (reservas is null || reservas.Count == 0)
            {
                Logger.Info($"[ObtenerReservasUsuario] UsuarioId='{request.UsuarioId}' no tiene reservas registradas.");
                return new List<ReservaUsuarioDTO>();
            }

            Logger.Debug($"[ObtenerReservasUsuario] Se encontraron {reservas.Count} reserva(s) para UsuarioId='{request.UsuarioId}'.");

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

            Logger.Debug($"[ObtenerReservasUsuario] DTOs construidos correctamente. Total={resultado.Count}.");

            return resultado;
        }
    }
}
