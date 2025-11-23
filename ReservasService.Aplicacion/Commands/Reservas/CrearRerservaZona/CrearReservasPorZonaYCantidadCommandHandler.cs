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
using ReservasService.Dominio.ValueObjects;

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

            // 2) Tomar N asientos y mapearlos al formato que espera el dominio
            var asientosSeleccionados = asientosDisponibles
                .Take(request.CantidadBoletos)
                .Select(a => (
                    asientoId: new Id(a.AsientoId, "AsientoId"),
                    precioUnitario: a.PrecioUnitario,
                    label: a.label
                ))
                .ToList();

            // 3) Crear UNA sola reserva con varios asientos
            var reserva = Reserva.CrearHold(
                new Id(request.EventId, "EventId"),
                new Id(request.ZonaEventoId, "ZonaEventoId"),
                new Id(request.UsuarioId, "UsuarioId"),
                asientosSeleccionados,
                request.TiempoHold
            );

            // 4) Guardar la reserva (una sola vez)
            await _reservaRepository.CrearAsync(reserva, cancellationToken);

            // 5) Marcar todos los asientos como "hold" en el MS de eventos
            foreach (var asiento in asientosSeleccionados)
            {
                await _asientosService.ActualizarEstadoAsync(
                    request.EventId,
                    request.ZonaEventoId,
                    asiento.asientoId.Value,
                    "hold",
                    cancellationToken);
            }

            // 6) Armar respuesta: misma reservaId, distintos asientos
            var resultados = reserva.Asientos
                .Select(a => new ReservaHoldResultDTO(
                    reserva.Id,
                    a.AsientoId.Value,
                    reserva.ExpiraEn!.Value
                ))
                .ToList();

            return resultados;
        }
    }
}
