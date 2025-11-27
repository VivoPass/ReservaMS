using log4net;
using MediatR;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using System;
using System.Threading;
using System.Threading.Tasks;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;

namespace ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva
{
    public class ConfirmarReservaCommandHandler
        : IRequestHandler<ConfirmarReservaCommand, bool>
    {
        private readonly IReservaRepository ReservaRepository;
        private readonly IAsientosDisponibilidadService AsientosService;
        private readonly ILog Logger;

        public ConfirmarReservaCommandHandler(
            IReservaRepository reservaRepository,
            IAsientosDisponibilidadService asientosService,
            ILog logger)
        {
            ReservaRepository = reservaRepository
                ?? throw new ReservaRepositoryNullException();

            AsientosService = asientosService
                ?? throw new AsientosDisponibilidadServiceNullException();

            Logger = logger ?? throw new LoggerNullException();
        }

        public async Task<bool> Handle(
            ConfirmarReservaCommand request,
            CancellationToken cancellationToken)
        {
            Logger.Info($"[ConfirmarReserva] Iniciando proceso para ReservaId='{request.ReservaId}'.");

            try
            {

                Logger.Debug($"Buscando la reserva en MongoDB. ReservaId='{request.ReservaId}'.");

                var reserva = await ReservaRepository.ObtenerPorIdAsync(
                    request.ReservaId,
                    cancellationToken);

                if (reserva is null)
                {
                    Logger.Warn($"Confirmación cancelada. ReservaId='{request.ReservaId}' no existe.");
                    throw new ReservaNotFoundException(request.ReservaId);
                }


                Logger.Debug($"Reserva encontrada. Estado actual='{reserva.Estado}'. Confirmando...");
                reserva.Confirmar();
                Logger.Debug($"Reserva confirmada en memoria. Nuevo estado='{reserva.Estado}'.");


                Logger.Info($"Actualizando estado de {reserva.Asientos.Count} asiento(s) a 'ocupado' en MS-Eventos.");

                foreach (var asiento in reserva.Asientos)
                {
                    Logger.Debug(
                        $"Actualizando asiento. AsientoId='{asiento.AsientoId.Value}', Zona='{reserva.ZonaEventoId.Value}', Evento='{reserva.EventId.Value}'.");

                    await AsientosService.ActualizarEstadoAsync(
                        reserva.EventId.Value,
                        reserva.ZonaEventoId.Value,
                        asiento.AsientoId.Value,
                        "ocupado",
                        cancellationToken);
                }

                Logger.Debug($"Persistiendo cambios en MongoDB para ReservaId='{reserva.Id}'.");
                await ReservaRepository.ActualizarAsync(reserva, cancellationToken);

                Logger.Info($"ReservaId='{reserva.Id}' confirmada y actualizada exitosamente.");
                return true;
            }
            catch (ReservaNotFoundException)
            {
                throw; 
            }
            catch (Exception ex)
            {
                Logger.Error($"[ConfirmarReserva] Error crítico al procesar ReservaId='{request.ReservaId}'.", ex);
                throw new ConfirmarReservaCommandHandlerException(ex);
            }
        }
    }
}
