using MediatR;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva
{
    public class ConfirmarReservaCommandHandler
        : IRequestHandler<ConfirmarReservaCommand, bool>
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly IAsientosDisponibilidadService _asientosService;

        public ConfirmarReservaCommandHandler(
            IReservaRepository reservaRepository,
            IAsientosDisponibilidadService asientosService)
        {
            _reservaRepository = reservaRepository;
            _asientosService = asientosService;
        }

        public async Task<bool> Handle(
            ConfirmarReservaCommand request,
            CancellationToken cancellationToken)
        {
            var reserva = await _reservaRepository.ObtenerPorIdAsync(
                request.ReservaId,
                cancellationToken);

            if (reserva is null)
                throw new InvalidOperationException("La reserva no existe.");

            // marca la reserva como confirmada (pagada)
            reserva.Confirmar();

            // actualizar estado de todos los asientos en el MS de eventos
            foreach (var asiento in reserva.Asientos)
            {
                await _asientosService.ActualizarEstadoAsync(
                    reserva.EventId.Value,
                    reserva.ZonaEventoId.Value,
                    asiento.AsientoId.Value,
                    "ocupado",               // o el estado que uses para pagado
                    cancellationToken);
            }

            await _reservaRepository.ActualizarAsync(reserva, cancellationToken);

            return true;
        }
    }
}
