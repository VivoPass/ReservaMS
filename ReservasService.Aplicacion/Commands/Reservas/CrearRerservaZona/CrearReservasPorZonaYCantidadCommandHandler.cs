using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;

using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;

using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;

namespace ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona
{
    public class CrearReservasPorZonaYCantidadCommandHandler
        : IRequestHandler<CrearReservasPorZonaYCantidadCommand, List<ReservaHoldResultDTO>>
    {
        private readonly IReservaRepository ReservaRepository;
        private readonly IAsientosDisponibilidadService AsientosService;
        private readonly ILog Logger;

        public CrearReservasPorZonaYCantidadCommandHandler(
            IReservaRepository reservaRepository,
            IAsientosDisponibilidadService asientosService,
            ILog logger)
        {
            ReservaRepository = reservaRepository ?? throw new ReservaRepositoryNullException();
            AsientosService = asientosService ?? throw new AsientosDisponibilidadServiceNullException();
            Logger = logger ?? throw new LoggerNullException();
        }

        public async Task<List<ReservaHoldResultDTO>> Handle(
            CrearReservasPorZonaYCantidadCommand request,
            CancellationToken cancellationToken)
        {
            Logger.Info($"[CrearReservasPorZona] Inicio. EventId='{request.EventId}', ZonaId='{request.ZonaEventoId}', UsuarioId='{request.UsuarioId}', CantidadBoletos={request.CantidadBoletos}.");

            try
            {

                if (request.CantidadBoletos <= 0)
                {
                    Logger.Warn($"[CrearReservasPorZona] Cantidad inválida: {request.CantidadBoletos}. Debe ser > 0.");
                    throw new ArgumentException("La cantidad de boletos debe ser mayor que cero.", nameof(request.CantidadBoletos));
                }

                Logger.Info("[CrearReservasPorZona] Consultando asientos disponibles en EventsService.");

                var asientosDisponibles = await AsientosService.ObtenerAsientosDisponiblesAsync(
                    request.EventId,
                    request.ZonaEventoId,
                    request.CantidadBoletos,
                    cancellationToken);

                Logger.Debug($"[CrearReservasPorZona] EventsService retornó {asientosDisponibles.Count} asientos posibles.");

                if (asientosDisponibles.Count < request.CantidadBoletos)
                {
                    Logger.Warn($"[CrearReservasPorZona] No hay suficientes asientos. Requeridos={request.CantidadBoletos}, Disponibles={asientosDisponibles.Count}.");
                    throw new NoHayAsientosSuficientesException(
                        request.EventId,
                        request.ZonaEventoId,
                        request.CantidadBoletos,
                        asientosDisponibles.Count);
                }


                Logger.Debug("[CrearReservasPorZona] Seleccionando y mapeando asientos para crear la reserva.");

                var asientosSeleccionados = asientosDisponibles
                    .Take(request.CantidadBoletos)
                    .Select(a => (
                        asientoId: new Id(a.AsientoId, "AsientoId"),
                        precioUnitario: a.PrecioUnitario,
                        label: a.label
                    ))
                    .ToList();

                Logger.Debug("[CrearReservasPorZona] Creando entidad de Reserva en modo HOLD.");

                var reserva = Reserva.CrearHold(
                    new Id(request.EventId, "EventId"),
                    new Id(request.ZonaEventoId, "ZonaEventoId"),
                    new Id(request.UsuarioId, "UsuarioId"),
                    asientosSeleccionados,
                    request.TiempoHold
                );

                Logger.Info($"[CrearReservasPorZona] Reserva HOLD creada en memoria. ReservaId='{reserva.Id}', ExpiraEn='{reserva.ExpiraEn}'.");


                Logger.Debug("[CrearReservasPorZona] Persistiendo reserva en MongoDB.");
                await ReservaRepository.CrearAsync(reserva, cancellationToken);
                Logger.Info($"[CrearReservasPorZona] ReservaId='{reserva.Id}' persistida correctamente.");

                Logger.Info($"[CrearReservasPorZona] Actualizando estado de {asientosSeleccionados.Count} asiento(s) a 'hold' en EventsService.");

                foreach (var asiento in asientosSeleccionados)
                {
                    Logger.Debug($"[CrearReservasPorZona] PUT estado='hold' AsientoId='{asiento.asientoId.Value}', Evento='{request.EventId}', Zona='{request.ZonaEventoId}'.");

                    await AsientosService.ActualizarEstadoAsync(
                        request.EventId,
                        request.ZonaEventoId,
                        asiento.asientoId.Value,
                        "hold",
                        cancellationToken);
                }
                var resultados = reserva.Asientos
                    .Select(a => new ReservaHoldResultDTO(
                        reserva.Id,
                        a.AsientoId.Value,
                        reserva.ExpiraEn!.Value
                    ))
                    .ToList();

                Logger.Info($"[CrearReservasPorZona] Proceso completado. ReservaId='{reserva.Id}', Asientos={resultados.Count}.");
                return resultados;
            }
            catch (NoHayAsientosSuficientesException)
            {
                // Ya se logueó arriba con Warn.
                throw;
            }
            catch (ArgumentException)
            {
                // Validación de entrada: también ya está logueado.
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("[CrearReservasPorZona] Error crítico durante la creación de reserva con HOLD.", ex);
                throw new CrearReservasPorZonaYCantidadCommandHandlerException(ex);
            }
        }
    }
}
