using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReservasService.Api.Contracts;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;

namespace ReservasService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservasController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReservasController(IMediator mediator)
        {
            _mediator = mediator;
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

            return Ok(response);
        }
    }
}
