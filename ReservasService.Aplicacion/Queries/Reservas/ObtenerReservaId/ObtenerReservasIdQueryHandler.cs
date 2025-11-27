using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId
{
    public class ObtenerReservasIdQueryHandler
        : IRequestHandler<ObtenerReservasIdQuery, ReservaUsuarioDTO?>
    {
        private readonly IReservaRepository ReservaRepository;
        private readonly ILog Logger;

        public ObtenerReservasIdQueryHandler(
            IReservaRepository reservaRepository,
            ILog logger)
        {
            ReservaRepository = reservaRepository ?? throw new ReservaRepositoryNullException();
            Logger = logger ?? throw new LoggerNullException();
        }

        public async Task<ReservaUsuarioDTO?> Handle(
            ObtenerReservasIdQuery request,
            CancellationToken cancellationToken)
        {
            Logger.Debug($"[ObtenerReservaId] Inicio. ReservaGuid='{request.ReservaGuid}'.");

            var r = await ReservaRepository.ObtenerPorIdAsync(
                request.ReservaGuid,
                cancellationToken);

            if (r is null)
            {
                Logger.Warn($"[ObtenerReservaId] No se encontró la reserva con ID='{request.ReservaGuid}'.");
                return null; // si luego quieres, aquí puedes cambiar a lanzar excepción
            }

            Logger.Debug($"[ObtenerReservaId] Reserva encontrada. ReservaId='{r.Id}', UsuarioId='{r.UsuarioId.Value}'.");

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

            Logger.Debug($"[ObtenerReservaId] DTO construido correctamente para ReservaId='{dto.ReservaId}'.");

            return dto;
        }
    }
}
