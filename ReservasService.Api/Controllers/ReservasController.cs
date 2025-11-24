using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReservasService.Api.Contracts;
using ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario;
using log4net;

namespace ReservasService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservasController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILog _logger;

        public ReservasController(IMediator mediator, IHttpClientFactory httpClientFactory, ILog logger)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Crea holds de reserva para una zona y cantidad de boletos.
        /// </summary>
        [HttpPost("hold")]
        public async Task<ActionResult<List<ReservaHoldResponse>>> CrearHoldPorZona(
            [FromBody] CrearReservaPorZonaRequest request,
            CancellationToken ct)
        {
            var command = new CrearReservasPorZonaYCantidadCommand
            {
                EventId = request.EventId,
                ZonaEventoId = request.ZonaEventoId,
                CantidadBoletos = request.CantidadBoletos,
                UsuarioId = request.UsuarioId
            };

            var result = await _mediator.Send(command, ct);

            var response = result.Select(x => new ReservaHoldResponse
            {
                ReservaId = x.ReservaId,
                AsientoId = x.AsientoId,
                ExpiraEn = x.ExpiraEn
            }).ToList();

            //Conexion con el MS de Usuarios para la publicacion de la actividad
            var client = _httpClientFactory.CreateClient("UsuariosClient");
            var activityBody = new PublishActivityRequest
            {
                idUsuario = request.UsuarioId.ToString(),
                accion = $"Reserva creada para el evento {request.EventId} con la " +
                         $"cantidad de boletos de {request.CantidadBoletos}."
            };
            const string endpoint = "/api/Usuarios/publishActivity";
            var httpResponse = await client.PostAsJsonAsync(endpoint, activityBody, ct);
            if (!httpResponse.IsSuccessStatusCode)
                Console.WriteLine($"[ADVERTENCIA] Falló la publicación de actividad para" +
                                  $" usuario {request.UsuarioId}. Status: {httpResponse.StatusCode}");

            return Ok(response);
        }

        /// <summary>
        /// Obtiene una reserva por su Id.
        /// </summary>
        [HttpGet("{reservaId:guid}")]
        public async Task<ActionResult<ReservaUsuarioDTO>> ObtenerReservaPorId(
            Guid reservaId,
            CancellationToken ct)
        {
            var query = new ObtenerReservasIdQuery(reservaId);

            var result = await _mediator.Send(query, ct);

            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las reservas de un usuario.
        /// </summary>
        [HttpGet("usuario/{usuarioId:guid}")]
        public async Task<ActionResult<List<ReservaUsuarioDTO>>> ObtenerReservasPorUsuario(
            Guid usuarioId,
            CancellationToken ct)
        {
            var query = new ObtenerReservasPorUsuarioQuery(usuarioId);

            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        /// <summary>
        /// Confirma una reserva existente.
        /// </summary>
        [HttpPost("{reservaId:guid}/confirmar")]
        public async Task<IActionResult> ConfirmarReserva(
            Guid reservaId,
            CancellationToken ct)
        {
            var command = new ConfirmarReservaCommand(reservaId);

            var exito = await _mediator.Send(command, ct);

            //Conexion con el MS de Usuarios para la publicacion de la actividad
            var query = new ObtenerReservasIdQuery(reservaId);
            var result = await _mediator.Send(query, ct);
            var client = _httpClientFactory.CreateClient("UsuariosClient");
            var activityBody = new PublishActivityRequest
            {
                idUsuario = result.UsuarioId.ToString(),
                accion = $"Reserva confirmada para el evento {result.EventId}."
            };
            const string endpoint = "/api/Usuarios/publishActivity";
            var httpResponse = await client.PostAsJsonAsync(endpoint, activityBody, ct);
            if (!httpResponse.IsSuccessStatusCode)
                System.Console.WriteLine($"[ADVERTENCIA] Falló la publicación de actividad para " +
                                         $"usuario {result.UsuarioId}. Status: {httpResponse.StatusCode}");

            if (!exito)
                return BadRequest("No se pudo confirmar la reserva.");

            return NoContent();
        }
    }
}
