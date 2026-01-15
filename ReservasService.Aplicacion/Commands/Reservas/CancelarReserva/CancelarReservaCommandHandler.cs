using log4net;
using MediatR;
using ReservasService.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReservasService.Dominio.Excepciones.Api;
using ReservasService.Dominio.Interfacess;

namespace ReservasService.Aplicacion.Commands.Reservas.CancelarReserva
{
    public class CancelarReservaCommandHandler : IRequestHandler<CancelarReservaCommand, bool>
    {
        private readonly IReservaRepository _reservasRepo;
        private readonly IAsientosDisponibilidadService _asientosService;
        private readonly ILog _logger;

        public CancelarReservaCommandHandler(
            IReservaRepository reservasRepo,
            IAsientosDisponibilidadService asientosService,
            ILog logger)
        {
            _reservasRepo = reservasRepo ?? throw new ArgumentNullException(nameof(reservasRepo));
            _asientosService = asientosService ?? throw new ArgumentNullException(nameof(asientosService));
            _logger = logger ?? throw new LoggerNullException();
        }

        public async Task<bool> Handle(CancelarReservaCommand request, CancellationToken cancellationToken)
        {
            _logger.Info($"[CancelarReserva] Iniciando cancelación de reserva {request.ReservaId}");

            // 1. Obtener la reserva
            var reserva = await _reservasRepo.ObtenerPorIdAsync(request.ReservaId)
                ?? throw new KeyNotFoundException("La reserva no existe.");

            // 2. Ejecutar la regla de dominio
            reserva.Cancelar();

            // 3. Actualizar estado de los asientos en EventsService
            foreach (var asiento in reserva.Asientos)
            {
                try
                {
                    await _asientosService.ActualizarEstadoAsync(
                        reserva.EventId.Value,
                        reserva.ZonaEventoId.Value,
                        asiento.AsientoId.Value,
                        "disponible",
                        cancellationToken
                    );
                }
                catch (Exception exAsiento)
                {
                    _logger.Error($"[CancelarReserva] Error liberando asiento {asiento.AsientoId}.", exAsiento);
                    // Puedes decidir si lanzar o continuar. Aquí continuamos.
                }
            }

            // 4. Persistir la reserva cancelada
            await _reservasRepo.ActualizarAsync(reserva, cancellationToken);

            _logger.Info($"[CancelarReserva] Reserva {request.ReservaId} cancelada correctamente.");

            return true;
        }
    }

}
