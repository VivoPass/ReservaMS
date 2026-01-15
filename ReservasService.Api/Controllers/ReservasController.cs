using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ReservasService.Api.Contracts;
using ReservasService.Aplicacion.Commands.Reservas.CancelarReserva;
using ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerTodas;
using ReservasService.Dominio.Excepciones.Api;
using LoggerNullException = ReservasService.Dominio.Excepciones.Infraestructura.LoggerNullException;

namespace ReservasService.Api.Controllers
{
    /// <summary>
    /// Controlador API para la gestión de reservas (holds, confirmaciones y consultas).
    /// </summary>
    /// <remarks>
    /// Este controlador actúa como puerta de entrada al microservicio de Reservas,
    /// utilizando MediatR para separar comandos (escritura) y queries (lectura),
    /// e integrándose con el microservicio de Usuarios para publicar actividad.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class ReservasController : ControllerBase
    {
        private readonly IMediator Mediator;
        private readonly IHttpClientFactory HttpClientFactory;
        private readonly ILog Logger;

        public ReservasController(
            IMediator mediator,
            IHttpClientFactory httpClientFactory,
            ILog logger)
        {
            Mediator = mediator ?? throw new MediatorNullException();
            HttpClientFactory = httpClientFactory ?? throw new HttpClientFactoryNullException();
            Logger = logger ?? throw new LoggerNullException();
        }

        #region CrearHoldPorZona([FromBody] CrearReservaPorZonaRequest request)
        /// <summary>
        /// Crea un hold de reserva para una zona y cantidad de boletos.
        /// </summary>
        /// <param name="request">Datos de la reserva (evento, zona, usuario y cantidad de boletos).</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>
        /// Lista de asientos en hold con su fecha de expiración.
        /// </returns>
        [HttpPost("hold")]
        [ProducesResponseType(typeof(List<ReservaHoldResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<List<ReservaHoldResponse>>> CrearHoldPorZona(
            [FromBody] CrearReservaPorZonaRequest request,
            CancellationToken ct)
        {
            Logger.Info($"[ReservasController] POST /hold iniciado. EventId='{request.EventId}', ZonaId='{request.ZonaEventoId}', UsuarioId='{request.UsuarioId}', Cantidad={request.CantidadBoletos}.");

            try
            {
                var command = new CrearReservasPorZonaYCantidadCommand
                {
                    EventId = request.EventId,
                    ZonaEventoId = request.ZonaEventoId,
                    CantidadBoletos = request.CantidadBoletos,
                    UsuarioId = request.UsuarioId
                };

                var result = await Mediator.Send(command, ct);

                if (result == null || result.Count == 0)
                {
                    Logger.Warn("[ReservasController] CrearReservasPorZonaYCantidadCommand retornó una lista vacía.");
                    return BadRequest("No se pudieron generar reservas en hold.");
                }

                var response = result.Select(x => new ReservaHoldResponse
                {
                    ReservaId = x.ReservaId,
                    AsientoId = x.AsientoId,
                    ExpiraEn = x.ExpiraEn
                }).ToList();

                Logger.Debug($"[ReservasController] Se generaron {response.Count} hold(s) de reserva para el usuario '{request.UsuarioId}'.");

                // Conexión con el MS de Usuarios para la publicación de la actividad
                try
                {
                    var client = HttpClientFactory.CreateClient("UsuariosClient");
                    var activityBody = new PublishActivityRequest
                    {
                        idUsuario = request.UsuarioId.ToString(),
                        accion = $"Reserva creada en HOLD para el evento {request.EventId} con {request.CantidadBoletos} boleto(s)."
                    };

                    const string endpoint = "/api/Usuarios/publishActivity";
                    Logger.Debug($"[ReservasController] Publicando actividad en Usuarios: UsuarioId='{activityBody.idUsuario}', Acción='{activityBody.accion}'.");

                    var httpResponse = await client.PostAsJsonAsync(endpoint, activityBody, ct);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        Logger.Warn($"[ReservasController] Falló la publicación de actividad para usuario {request.UsuarioId}. Status={httpResponse.StatusCode}.");
                    }
                    else
                    {
                        Logger.Info($"[ReservasController] Actividad publicada correctamente para usuario {request.UsuarioId}.");
                    }
                }
                catch (Exception exPub)
                {
                    Logger.Error($"[ReservasController] Error al publicar actividad en Usuarios para usuario {request.UsuarioId}.", exPub);
                    // No rompemos el flujo de reservas si solo falla la actividad.
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                Logger.Error("[ReservasController] Error al crear hold de reserva.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region ObtenerReservaPorId(Guid reservaId)
        /// <summary>
        /// Obtiene una reserva por su identificador.
        /// </summary>
        /// <param name="reservaId">ID de la reserva.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Reserva con sus asientos y datos básicos.</returns>
        [HttpGet("{reservaId:guid}")]
        [ProducesResponseType(typeof(ReservaUsuarioDTO), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ReservaUsuarioDTO>> ObtenerReservaPorId(
            Guid reservaId,
            CancellationToken ct)
        {
            Logger.Debug($"[ReservasController] GET /{{reservaId}} iniciado. ReservaId='{reservaId}'.");

            try
            {
                var query = new ObtenerReservasIdQuery(reservaId);
                var result = await Mediator.Send(query, ct);

                if (result is null)
                {
                    Logger.Warn($"[ReservasController] No se encontró reserva con ID='{reservaId}'.");
                    return NotFound($"No se encontró una reserva con el ID {reservaId}.");
                }

                Logger.Debug($"[ReservasController] Reserva encontrada. ReservaId='{result.ReservaId}', UsuarioId='{result.UsuarioId}'.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"[ReservasController] Error al obtener reserva por ID '{reservaId}'.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region ObtenerReservasPorUsuario(Guid usuarioId)
        /// <summary>
        /// Obtiene todas las reservas asociadas a un usuario.
        /// </summary>
        /// <param name="usuarioId">ID del usuario.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Lista de reservas del usuario.</returns>
        [HttpGet("usuario/{usuarioId:guid}")]
        [ProducesResponseType(typeof(List<ReservaUsuarioDTO>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<List<ReservaUsuarioDTO>>> ObtenerReservasPorUsuario(
            Guid usuarioId,
            CancellationToken ct)
        {
            Logger.Debug($"[ReservasController] GET /usuario/{{usuarioId}} iniciado. UsuarioId='{usuarioId}'.");

            try
            {
                var query = new ObtenerReservasPorUsuarioQuery(usuarioId);
                var result = await Mediator.Send(query, ct);

                if (result == null || result.Count == 0)
                {
                    Logger.Info($"[ReservasController] UsuarioId='{usuarioId}' no tiene reservas registradas.");
                    // Devolvemos 200 con lista vacía (es una decisión de diseño razonable para listados).
                    return Ok(new List<ReservaUsuarioDTO>());
                }

                Logger.Debug($"[ReservasController] Se encontraron {result.Count} reserva(s) para UsuarioId='{usuarioId}'.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"[ReservasController] Error al obtener reservas por usuario '{usuarioId}'.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion

        #region ConfirmarReserva(Guid reservaId)
        /// <summary>
        /// Confirma una reserva existente (cambia asientos a ocupados y marca la reserva como confirmada).
        /// </summary>
        /// <param name="reservaId">ID de la reserva a confirmar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>NoContent si se confirma correctamente.</returns>
        [HttpPost("{reservaId:guid}/confirmar")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> ConfirmarReserva(
            Guid reservaId,
            CancellationToken ct)
        {
            Logger.Info($"[ReservasController] POST /{{reservaId}}/confirmar iniciado. ReservaId='{reservaId}'.");

            try
            {
                var command = new ConfirmarReservaCommand(reservaId);
                var exito = await Mediator.Send(command, ct);

                if (!exito)
                {
                    Logger.Warn($"[ReservasController] ConfirmarReservaCommand retornó false para ReservaId='{reservaId}'.");
                    return BadRequest("No se pudo confirmar la reserva.");
                }

                Logger.Info($"[ReservasController] ReservaId='{reservaId}' confirmada correctamente. Intentando publicar actividad en Usuarios.");

                // Obtener datos de la reserva para poder publicar actividad (usuario, evento)
                ReservaUsuarioDTO? reserva = null;
                try
                {
                    var query = new ObtenerReservasIdQuery(reservaId);
                    reserva = await Mediator.Send(query, ct);
                }
                catch (Exception exQuery)
                {
                    Logger.Error($"[ReservasController] Error al obtener la reserva confirmada para publicar actividad. ReservaId='{reservaId}'.", exQuery);
                }

                if (reserva != null)
                {
                    try
                    {
                        var client = HttpClientFactory.CreateClient("UsuariosClient");
                        var activityBody = new PublishActivityRequest
                        {
                            idUsuario = reserva.UsuarioId.ToString(),
                            accion = $"Reserva confirmada para el evento {reserva.EventId}."
                        };

                        const string endpoint = "/api/Usuarios/publishActivity";
                        Logger.Debug($"[ReservasController] Publicando actividad de confirmación. UsuarioId='{activityBody.idUsuario}', Evento='{reserva.EventId}'.");

                        var httpResponse = await client.PostAsJsonAsync(endpoint, activityBody, ct);

                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            Logger.Warn($"[ReservasController] Falló la publicación de actividad de confirmación para usuario {reserva.UsuarioId}. Status={httpResponse.StatusCode}.");
                        }
                        else
                        {
                            Logger.Info($"[ReservasController] Actividad de confirmación publicada correctamente para usuario {reserva.UsuarioId}.");
                        }
                    }
                    catch (Exception exPub)
                    {
                        Logger.Error($"[ReservasController] Error al publicar actividad de confirmación para ReservaId='{reservaId}'.", exPub);
                    }
                }
                else
                {
                    Logger.Warn($"[ReservasController] No se pudo obtener la reserva '{reservaId}' tras confirmar para publicar actividad. Se continúa sin romper flujo.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ReservasController] Error al confirmar reserva '{reservaId}'.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        #endregion


        #region CancelarReserva(Guid reservaId)

        /// <summary>
        /// Cancela una reserva existente (libera asientos y marca la reserva como liberada).
        /// </summary>
        /// <param name="reservaId">ID de la reserva a cancelar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>NoContent si se cancela correctamente.</returns>
        [HttpDelete("{reservaId:guid}/cancelar")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> CancelarReserva(
            Guid reservaId,
            CancellationToken ct)
        {
            Logger.Info($"[ReservasController] DELETE /{{reservaId}}/cancelar iniciado. ReservaId='{reservaId}'.");

            try
            {
                // 1) Ejecutar command
                var command = new CancelarReservaCommand(reservaId);
                var exito = await Mediator.Send(command, ct);

                if (!exito)
                {
                    Logger.Warn($"[ReservasController] CancelarReservaCommand retornó false para ReservaId='{reservaId}'.");
                    return BadRequest("No se pudo cancelar la reserva.");
                }

                Logger.Info($"[ReservasController] ReservaId='{reservaId}' cancelada correctamente. Intentando publicar actividad en Usuarios.");

                // 2) Intentar obtener la reserva cancelada para publicar actividad
                ReservaUsuarioDTO? reserva = null;
                try
                {
                    var query = new ObtenerReservasIdQuery(reservaId);
                    reserva = await Mediator.Send(query, ct);
                }
                catch (Exception exQuery)
                {
                    Logger.Error($"[ReservasController] Error al obtener la reserva cancelada para publicar actividad. ReservaId='{reservaId}'.", exQuery);
                }

                // 3) Publicar actividad (solo si se pudo obtener la reserva)
                if (reserva != null)
                {
                    try
                    {
                        var client = HttpClientFactory.CreateClient("UsuariosClient");
                        var activityBody = new PublishActivityRequest
                        {
                            idUsuario = reserva.UsuarioId.ToString(),
                            accion = $"Reserva cancelada para el evento {reserva.EventId}."
                        };

                        const string endpoint = "/api/Usuarios/publishActivity";
                        Logger.Debug($"[ReservasController] Publicando actividad de cancelación. UsuarioId='{activityBody.idUsuario}', Evento='{reserva.EventId}'.");

                        var httpResponse = await client.PostAsJsonAsync(endpoint, activityBody, ct);

                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            Logger.Warn($"[ReservasController] Falló la publicación de actividad para usuario {reserva.UsuarioId}. Status={httpResponse.StatusCode}.");
                        }
                        else
                        {
                            Logger.Info($"[ReservasController] Actividad de cancelación publicada correctamente para usuario {reserva.UsuarioId}.");
                        }
                    }
                    catch (Exception exPub)
                    {
                        Logger.Error($"[ReservasController] Error al publicar actividad de cancelación para ReservaId='{reservaId}'.", exPub);
                    }
                }
                else
                {
                    Logger.Warn($"[ReservasController] No se pudo obtener la reserva '{reservaId}' tras cancelarla. Se continúa sin romper flujo.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ReservasController] Error al cancelar reserva '{reservaId}'.", ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        #endregion


        [HttpGet("todas")]
        public async Task<IActionResult> ObtenerTodasLasReservas()
        {
            var query = new ObtenerTodasLasReservasQuery();
            var result = await Mediator.Send(query);
            return Ok(result);
        }


    }
}
