using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReservasService.Api.Contracts;
using ReservasService.Api.Controllers;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Aplicacion.DTOS;
using Xunit;

namespace Reservas.Tests.Reservas.API.Controller
{
    public class ReservasController_CrearHoldPorZona_Tests
    {
        private readonly Mock<IMediator> MockMediator;
        private readonly Mock<IHttpClientFactory> MockHttpClientFactory;
        private readonly Mock<ILog> MockLogger;
        private readonly ReservasController Controller;

        // --- DATOS ---
        private readonly Guid TestEventoId = Guid.NewGuid();
        private readonly Guid TestZonaId = Guid.NewGuid();
        private readonly Guid TestUsuarioId = Guid.NewGuid();

        private readonly CrearReservaPorZonaRequest ValidRequest;

        public ReservasController_CrearHoldPorZona_Tests()
        {
            MockMediator = new Mock<IMediator>();
            MockHttpClientFactory = new Mock<IHttpClientFactory>();
            MockLogger = new Mock<ILog>();

            // HttpClient fake para que la llamada a Usuarios no haga request real
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)
            ))
            {
                BaseAddress = new Uri("http://localhost")
            };

            MockHttpClientFactory
                .Setup(f => f.CreateClient("UsuariosClient"))
                .Returns(httpClient);

            Controller = new ReservasController(
                MockMediator.Object,
                MockHttpClientFactory.Object,
                MockLogger.Object
            );

            // --- DATOS ---
            ValidRequest = new CrearReservaPorZonaRequest
            {
                EventId = TestEventoId,
                ZonaEventoId = TestZonaId,
                UsuarioId = TestUsuarioId,
                CantidadBoletos = 2
            };
        }

        #region Exito_ShouldReturn200OKAndMapResponse()
        [Fact]
        public async Task CrearHoldPorZona_ReservasGeneradas_ShouldReturn200OKAndMappedResponse()
        {
            // ARRANGE
            var reservasGeneradas = new List<ReservaHoldResultDTO>
            {
                new ReservaHoldResultDTO(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddMinutes(10)
                ),
                new ReservaHoldResultDTO(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddMinutes(15)
                )
            };

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservasGeneradas);

            // ACT
            var result = await Controller.CrearHoldPorZona(ValidRequest, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var responseBody = Assert.IsType<List<ReservaHoldResponse>>(okResult.Value);
            Assert.Equal(reservasGeneradas.Count, responseBody.Count);

            MockMediator.Verify(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region ListaVacia_ShouldReturn400BadRequest()
        [Fact]
        public async Task CrearHoldPorZona_SinReservasGeneradas_ShouldReturn400BadRequest()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ReservaHoldResultDTO>());

            // ACT
            var result = await Controller.CrearHoldPorZona(ValidRequest, CancellationToken.None);

            // ASSERT
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("No se pudieron generar reservas en hold.", badRequestResult.Value);

            MockMediator.Verify(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Excepcion_ShouldReturn500InternalServerError()
        [Fact]
        public async Task CrearHoldPorZona_MediatorLanzaExcepcion_ShouldReturn500InternalServerError()
        {
            // ARRANGE
            var exception = new Exception("Error al generar reservas en hold.");

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // ACT
            var result = await Controller.CrearHoldPorZona(ValidRequest, CancellationToken.None);

            // ASSERT
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal(exception.Message, statusCodeResult.Value);
        }
        #endregion

        #region Clases de apoyo
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public FakeHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
        #endregion

        #region PublicacionActividad_StatusNoExitoso_ShouldReturn200OK()
        [Fact]
        public async Task CrearHoldPorZona_ReservasGeneradas_PublicacionActividadStatusNoExitoso_ShouldReturn200OK()
        {
            // ARRANGE
            var reservasGeneradas = new List<ReservaHoldResultDTO>
            {
                new ReservaHoldResultDTO(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10))
            };

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservasGeneradas);

            // HttpClient que devuelve 500 para cubrir rama !IsSuccessStatusCode
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            ))
            {
                BaseAddress = new Uri("http://localhost")
            };

            MockHttpClientFactory
                .Setup(f => f.CreateClient("UsuariosClient"))
                .Returns(httpClient);

            // ACT
            var result = await Controller.CrearHoldPorZona(ValidRequest, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var body = Assert.IsType<List<ReservaHoldResponse>>(okResult.Value);
            Assert.Single(body);
        }
        #endregion

        #region PublicacionActividad_LanzaExcepcion_ShouldReturn200OK()
        [Fact]
        public async Task CrearHoldPorZona_ReservasGeneradas_PublicacionActividadLanzaExcepcion_ShouldReturn200OK()
        {
            // ARRANGE
            var reservasGeneradas = new List<ReservaHoldResultDTO>
            {
                new ReservaHoldResultDTO(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddMinutes(10))
            };

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<CrearReservasPorZonaYCantidadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservasGeneradas);

            // HttpClient cuyo handler lanza excepción para cubrir catch (Exception exPub)
            var httpClient = new HttpClient(new ThrowingHttpMessageHandler())
            {
                BaseAddress = new Uri("http://localhost")
            };

            MockHttpClientFactory
                .Setup(f => f.CreateClient("UsuariosClient"))
                .Returns(httpClient);

            // ACT
            var result = await Controller.CrearHoldPorZona(ValidRequest, CancellationToken.None);

            // ASSERT
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);

            var body = Assert.IsType<List<ReservaHoldResponse>>(okResult.Value);
            Assert.Single(body);
        }
        #endregion

        private class ThrowingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                throw new Exception("Error al publicar actividad (simulado)");
            }
        }

    }
}
