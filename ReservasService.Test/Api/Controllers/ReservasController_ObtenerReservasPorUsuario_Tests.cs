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
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario;
using Xunit;

namespace Reservas.Tests.Reservas.API.Controller
{
    public class ReservasController_ObtenerReservasPorUsuario_Tests
    {
        private readonly Mock<IMediator> MockMediator;
        private readonly Mock<IHttpClientFactory> MockHttpClientFactory;
        private readonly Mock<ILog> MockLogger;
        private readonly ReservasController Controller;

        private readonly Guid TestUsuarioId = Guid.NewGuid();

        public ReservasController_ObtenerReservasPorUsuario_Tests()
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

        #region ExitoConResultados_ShouldReturn200OK()
        [Fact]
        public async Task ObtenerReservasPorUsuario_ConReservas_ShouldReturn200OKWithList()
        {
            // ARRANGE
            var reservas = new List<ReservaUsuarioDTO>
            {
                new ReservaUsuarioDTO(),
                new ReservaUsuarioDTO()
            };

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasPorUsuarioQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservas);

            // ACT
            var result = await Controller.ObtenerReservasPorUsuario(TestUsuarioId, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var body = Assert.IsType<List<ReservaUsuarioDTO>>(okResult.Value);
            Assert.Equal(2, body.Count);
        }
        #endregion

        #region SinResultados_ShouldReturn200OKConListaVacia()
        [Fact]
        public async Task ObtenerReservasPorUsuario_SinReservas_ShouldReturn200OKWithEmptyList()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasPorUsuarioQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ReservaUsuarioDTO>());

            // ACT
            var result = await Controller.ObtenerReservasPorUsuario(TestUsuarioId, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var body = Assert.IsType<List<ReservaUsuarioDTO>>(okResult.Value);
            Assert.Empty(body);
        }
        #endregion

        #region Excepcion_ShouldReturn500()
        [Fact]
        public async Task ObtenerReservasPorUsuario_MediatorLanzaExcepcion_ShouldReturn500InternalServerError()
        {
            // ARRANGE
            var exception = new Exception("Error al obtener reservas del usuario.");

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasPorUsuarioQuery>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // ACT
            var result = await Controller.ObtenerReservasPorUsuario(TestUsuarioId, CancellationToken.None);

            // ASSERT
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal(exception.Message, statusCodeResult.Value);
        }
        #endregion
    }
}
