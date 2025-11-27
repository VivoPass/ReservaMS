using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReservasService.Api.Controllers;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId;
using Xunit;

namespace Reservas.Tests.Reservas.API.Controller
{
    public class ReservasController_ObtenerReservaPorId_Tests
    {
        private readonly Mock<IMediator> MockMediator;
        private readonly Mock<IHttpClientFactory> MockHttpClientFactory;
        private readonly Mock<ILog> MockLogger;
        private readonly ReservasController Controller;

        private readonly Guid TestReservaId = Guid.NewGuid();

        public ReservasController_ObtenerReservaPorId_Tests()
        {
            MockMediator = new Mock<IMediator>();
            MockHttpClientFactory = new Mock<IHttpClientFactory>();
            MockLogger = new Mock<ILog>();

            Controller = new ReservasController(
                MockMediator.Object,
                MockHttpClientFactory.Object,
                MockLogger.Object
            );
        }

        #region Exito_ShouldReturn200OK()
        [Fact]
        public async Task ObtenerReservaPorId_ReservaExiste_ShouldReturn200OK()
        {
            // ARRANGE
            var reserva = new ReservaUsuarioDTO(); // Asumiendo ctor por defecto. Si no, usa tu ctor real.

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reserva);

            // ACT
            var result = await Controller.ObtenerReservaPorId(TestReservaId, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.IsType<ReservaUsuarioDTO>(okResult.Value);
        }
        #endregion

        #region NotFound_ShouldReturn404()
        [Fact]
        public async Task ObtenerReservaPorId_ReservaNoExiste_ShouldReturn404NotFound()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ReservaUsuarioDTO?)null);

            // ACT
            var result = await Controller.ObtenerReservaPorId(TestReservaId, CancellationToken.None);

            // ASSERT
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);

            var mensaje = Assert.IsType<string>(notFoundResult.Value);
            Assert.Contains(TestReservaId.ToString(), mensaje);
        }
        #endregion

        #region Excepcion_ShouldReturn500()
        [Fact]
        public async Task ObtenerReservaPorId_MediatorLanzaExcepcion_ShouldReturn500InternalServerError()
        {
            // ARRANGE
            var exception = new Exception("Error al obtener reserva.");

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // ACT
            var result = await Controller.ObtenerReservaPorId(TestReservaId, CancellationToken.None);

            // ASSERT
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal(exception.Message, statusCodeResult.Value);
        }
        #endregion
    }
}
