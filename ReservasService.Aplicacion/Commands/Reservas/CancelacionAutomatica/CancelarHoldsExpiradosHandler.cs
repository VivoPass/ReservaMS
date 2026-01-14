using log4net;
using MediatR;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Commands.Reservas.CancelacionAutomatica
{
    public class CancelarHoldsExpiradosHandler
       : IRequestHandler<CancelarHoldsExpiradosCommand>
    {
        private readonly IReservaRepository _repo;
        private readonly IAsientosDisponibilidadService _asientosService;
        private readonly ILog _logger;

        public CancelarHoldsExpiradosHandler(
            IReservaRepository repo,
            IAsientosDisponibilidadService asientosService,
            ILog logger)
        {
            _repo = repo;
            _asientosService = asientosService;
            _logger = logger;
        }

        public async Task Handle(
            CancelarHoldsExpiradosCommand request,
            CancellationToken cancellationToken)
        {
            var ahora = DateTime.UtcNow;

            _logger.Info($"[AutoCancelación] Buscando reservas hold expiradas hasta '{ahora:O}'.");

            var expiradas = await _repo.ObtenerHoldsExpiradosHastaAsync(ahora, cancellationToken);

            if (expiradas.Count == 0)
            {
                _logger.Debug("[AutoCancelación] No hay holds expirados para procesar.");
                return;
            }

            foreach (var reserva in expiradas)
            {
                try
                {
                    // 1. Cambiar estado en dominio
                    reserva.MarcarComoExpirada();

                    // 2. Persistir
                    await _repo.ActualizarAsync(reserva, cancellationToken);

                    // 3. Liberar asientos en EventsService
                    foreach (var asiento in reserva.Asientos)
                    {
                        await _asientosService.ActualizarEstadoAsync(
                            reserva.EventId.Value,
                            reserva.ZonaEventoId.Value,
                            asiento.AsientoId.Value,
                            "disponible",
                            cancellationToken
                        );

                        _logger.Info($"[AutoCancelación] Asiento '{asiento.AsientoId.Value}' liberado en EventsService.");
                    }

                    _logger.Info($"[AutoCancelación] Reserva '{reserva.Id}' marcada como Expirada y asientos liberados.");
                }
                catch (Exception ex)
                {
                    _logger.Error($"[AutoCancelación] Error procesando reserva '{reserva.Id}'.", ex);
                }
            }
        }
    }

}
