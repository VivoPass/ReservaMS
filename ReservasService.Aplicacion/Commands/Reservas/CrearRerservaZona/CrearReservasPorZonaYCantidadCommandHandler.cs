using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;

namespace ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona
{
    public class CrearReservasPorZonaYCantidadCommandHandler
        : IRequestHandler<CrearReservasPorZonaYCantidadCommand, List<ReservaHoldResultDTO>>
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly IAsientosDisponibilidadService _asientosService;

        public CrearReservasPorZonaYCantidadCommandHandler(
            IReservaRepository reservaRepository,
            IAsientosDisponibilidadService asientosService)
        {
            _reservaRepository = reservaRepository;
            _asientosService = asientosService;
        }

        public async Task<List<ReservaHoldResultDTO>> Handle(
            CrearReservasPorZonaYCantidadCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CantidadBoletos <= 0)
                throw new ArgumentException("La cantidad de boletos debe ser mayor que cero.", nameof(request.CantidadBoletos));

            // 1) Pedir asientos disponibles al servicio de eventos
            var asientosDisponibles = await _asientosService.ObtenerAsientosDisponiblesAsync(
                request.EventId,
                request.ZonaEventoId,
                request.CantidadBoletos,
                cancellationToken);

            if (asientosDisponibles.Count < request.CantidadBoletos)
                throw new InvalidOperationException("No hay suficientes asientos disponibles en la zona seleccionada.");

            var resultados = new List<ReservaHoldResultDTO>();

            // 2) Crear holds por cada asiento
            foreach (var asientoId in asientosDisponibles.Take(request.CantidadBoletos))
            {
                var reserva = Reserva.CrearHold(
                    request.EventId,
                    request.ZonaEventoId,
                    asientoId,
                    request.UsuarioId,
                    request.TiempoHold
                );

                // Guardar la reserva en Mongo
                await _reservaRepository.CrearAsync(reserva, cancellationToken);

                // Cambiar estado del asiento en el MS de Eventos a "hold"
                await _asientosService.ActualizarEstadoAsync(
                    request.EventId,
                    request.ZonaEventoId,
                    asientoId,
                    "hold",
                    cancellationToken);

                resultados.Add(new ReservaHoldResultDTO(
                    reserva.Id,
                    reserva.AsientoId.Value,
                    reserva.ExpiraEn!.Value
                ));
            }

            return resultados;
        }
    }
}
