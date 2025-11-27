using System;
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
using ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId;
using Xunit;

namespace Reservas.Tests.Reservas.API.Controller
{
    public class ReservasController_ConfirmarReserva_Tests
    {
        private readonly Mock<IMediator> MockMediator;
        private readonly Mock<IHttpClientFactory> MockHttpClientFactory;
        private readonly Mock<ILog> MockLogger;
        private readonly ReservasController Controller;

        private readonly Guid TestReservaId = Guid.NewGuid();

        public ReservasController_ConfirmarReserva_Tests()
        {
            MockMediator = new Mock<IMediator>();
            MockHttpClientFactory = new Mock<IHttpClientFactory>();
            MockLogger = new Mock<ILog>();

            // HttpClient por defecto (éxito)
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
        }

        #region Exito_SinReservaParaActividad_ShouldReturn204NoContent()
        [Fact]
        public async Task ConfirmarReserva_ConfirmacionExitosa_SinReservaParaActividad_ShouldReturn204NoContent()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // No configuramos ObtenerReservasIdQuery => devuelve null por defecto

            // ACT
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        }
        #endregion

        #region FallaNegocio_ShouldReturn400BadRequest()
        [Fact]
        public async Task ConfirmarReserva_NoSePuedeConfirmar_ShouldReturn400BadRequest()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // ACT
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            Assert.Equal("No se pudo confirmar la reserva.", badRequestResult.Value);
        }
        #endregion

        #region Excepcion_ShouldReturn500InternalServerError()
        [Fact]
        public async Task ConfirmarReserva_MediatorLanzaExcepcion_ShouldReturn500InternalServerError()
        {
            // ARRANGE
            var exception = new Exception("Error al confirmar reserva.");

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // ACT
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal(exception.Message, statusCodeResult.Value);
        }
        #endregion

        #region Exito_ConReservaYPublicacionExitosa_ShouldReturn204NoContent()
        [Fact]
        public async Task ConfirmarReserva_ConfirmacionExitosa_ConReservaYPublicacionExitosa_ShouldReturn204NoContent()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Devolvemos una reserva para disparar el bloque de publicación
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReservaUsuarioDTO());

            // HttpClient ya está configurado con respuesta 200 OK en el ctor

            // ACT
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        }
        #endregion

        #region Exito_ConReservaYPublicacionStatusNoExitoso_ShouldReturn204NoContent()
        [Fact]
        public async Task ConfirmarReserva_ConfirmacionExitosa_ConReservaYPublicacionStatusNoExitoso_ShouldReturn204NoContent()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReservaUsuarioDTO());

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
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        }
        #endregion

        #region Exito_ConReservaYPublicacionLanzaExcepcion_ShouldReturn204NoContent()
        [Fact]
        public async Task ConfirmarReserva_ConfirmacionExitosa_ConReservaYPublicacionLanzaExcepcion_ShouldReturn204NoContent()
        {
            // ARRANGE
            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ConfirmarReservaCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            MockMediator
                .Setup(m => m.Send(
                    It.IsAny<ObtenerReservasIdQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReservaUsuarioDTO());

            // HttpClient cuyo handler lanza excepción para cubrir catch (Exception exPub)
            var httpClient = new HttpClient(new ThrowingHttpMessageHandler())
            {
                BaseAddress = new Uri("http://localhost")
            };

            MockHttpClientFactory
                .Setup(f => f.CreateClient("UsuariosClient"))
                .Returns(httpClient);

            // ACT
            var result = await Controller.ConfirmarReserva(TestReservaId, CancellationToken.None);

            // ASSERT
            var noContentResult = Assert.IsType<NoContentResult>(result);
            Assert.Equal(StatusCodes.Status204NoContent, noContentResult.StatusCode);
        }
        #endregion

        #region Helpers
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

        private class ThrowingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                throw new Exception("Error al publicar actividad (simulado)");
            }
        }
        #endregion
    }
}
