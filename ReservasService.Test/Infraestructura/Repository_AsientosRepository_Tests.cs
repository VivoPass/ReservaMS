using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Moq;
using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;
using ReservasService.Infraestructura.Repositorios;
using Xunit;

namespace ReservasService.Tests.Infraestructura.Repositorios
{
    public class Repository_AsientosRepository_Tests
    {
        private readonly Mock<ILog> MockLogger;

        public Repository_AsientosRepository_Tests()
        {
            MockLogger = new Mock<ILog>();
        }

        #region Clase interna: FakeHttpMessageHandler
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => _handler(request, cancellationToken);
        }
        #endregion

        // =====================================================================
        //  ObtenerAsientosDisponiblesAsync
        // =====================================================================

        #region ObtenerAsientosDisponibles_CantidadInvalida_DeberiaLanzarArgumentException
        [Fact]
        public async Task ObtenerAsientosDisponibles_CantidadInvalida_DeberiaLanzarArgumentException()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler((req, ct) =>
                throw new InvalidOperationException("No debería llamarse para cantidad <= 0."));
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var repo = new AsientosRepository(httpClient, MockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                repo.ObtenerAsientosDisponiblesAsync(Guid.NewGuid(), Guid.NewGuid(), 0, CancellationToken.None));
        }
        #endregion

        #region ObtenerAsientosDisponibles_FlujoFeliz_DeberiaRetornarListaOrdenadaYConPrecioZona
        [Fact]
        public async Task ObtenerAsientosDisponibles_FlujoFeliz_DeberiaRetornarListaOrdenadaYConPrecioZona()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var zonaId = Guid.NewGuid();
            var cantidadSolicitada = 2;

            var asientosRemotos = new List<AsientoRemotoDTO>
            {
                new AsientoRemotoDTO
                {
                    Id = Guid.NewGuid(),
                    Estado = "disponible",
                    FilaIndex = 2,
                    ColIndex = 5,
                    Label = "B-6"
                },
                new AsientoRemotoDTO
                {
                    Id = Guid.NewGuid(),
                    Estado = "ocupado",
                    FilaIndex = 1,
                    ColIndex = 1,
                    Label = "A-1"
                },
                new AsientoRemotoDTO
                {
                    Id = Guid.NewGuid(),
                    Estado = "disponible",
                    FilaIndex = 1,
                    ColIndex = 3,
                    Label = "A-4"
                }
            };

            var zonaDetalle = new ZonaDetalleResponse
            {
                Id = zonaId,
                Nombre = "VIP",
                Precio = 99.99m
            };

            var handler = new FakeHttpMessageHandler(async (request, ct) =>
            {
                var path = request.RequestUri!.PathAndQuery;

                // GET /api/events/{eventId}/zonas/{zonaId}/asientos
                if (request.Method == HttpMethod.Get &&
                    path.Contains($"/api/events/{eventId}/zonas/{zonaId}/asientos"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(asientosRemotos)
                    };
                }

                // GET /api/events/{eventId}/zonas/{zonaId}?includeSeats=true
                if (request.Method == HttpMethod.Get &&
                    path.Contains($"/api/events/{eventId}/zonas/{zonaId}") &&
                    path.Contains("includeSeats=true"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(zonaDetalle)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var repo = new AsientosRepository(httpClient, MockLogger.Object);

            // Act
            var resultado = await repo.ObtenerAsientosDisponiblesAsync(
                eventId, zonaId, cantidadSolicitada, CancellationToken.None);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(cantidadSolicitada, resultado.Count);

            // Deben venir ordenados por FilaIndex, luego ColIndex:
            // disponibles: (1,3) y (2,5)
            Assert.Equal(asientosRemotos[2].Id, resultado[0].AsientoId);
            Assert.Equal(asientosRemotos[0].Id, resultado[1].AsientoId);

            // Precio debe venir de la zona
            Assert.All(resultado, r => Assert.Equal(zonaDetalle.Precio, r.PrecioUnitario));

            // Label se respeta
            Assert.Equal(asientosRemotos[2].Label, resultado[0].label);
            Assert.Equal(asientosRemotos[0].Label, resultado[1].label);
        }
        #endregion

        #region ObtenerAsientosDisponibles_ErrorHttp_DeberiaRelanzarExcepcion
        [Fact]
        public async Task ObtenerAsientosDisponibles_ErrorHttp_DeberiaRelanzarExcepcion()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler((request, ct) =>
            {
                // Simulamos fallo en la llamada HTTP
                throw new HttpRequestException("Simulated HTTP error");
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var repo = new AsientosRepository(httpClient, MockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                repo.ObtenerAsientosDisponiblesAsync(Guid.NewGuid(), Guid.NewGuid(), 3, CancellationToken.None));
        }
        #endregion

        // =====================================================================
        //  ActualizarEstadoAsync
        // =====================================================================

        #region ActualizarEstado_FlujoFeliz_DeberiaHacerPutSinErrores
        [Fact]
        public async Task ActualizarEstado_FlujoFeliz_DeberiaHacerPutSinErrores()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var zonaId = Guid.NewGuid();
            var asientoId = Guid.NewGuid();
            var nuevoEstado = "hold";

            ActualizarAsientoRequest? bodyRecibido = null;

            var handler = new FakeHttpMessageHandler(async (request, ct) =>
            {
                if (request.Method == HttpMethod.Put)
                {
                    bodyRecibido = await request.Content!
                        .ReadFromJsonAsync<ActualizarAsientoRequest>(cancellationToken: ct);

                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var repo = new AsientosRepository(httpClient, MockLogger.Object);

            // Act
            await repo.ActualizarEstadoAsync(eventId, zonaId, asientoId, nuevoEstado, CancellationToken.None);

            // Assert
            Assert.NotNull(bodyRecibido);
            Assert.Equal(nuevoEstado, bodyRecibido!.Estado);
        }
        #endregion

        #region ActualizarEstado_RespuestaNoExitosa_DeberiaLanzarHttpRequestException
        [Fact]
        public async Task ActualizarEstado_RespuestaNoExitosa_DeberiaLanzarHttpRequestException()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler((request, ct) =>
            {
                // Simulamos que EventsService responde 500
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal error")
                };
                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            var repo = new AsientosRepository(httpClient, MockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                repo.ActualizarEstadoAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ocupado", CancellationToken.None));
        }
        #endregion
    }
}
