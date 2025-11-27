using log4net;
using Moq;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaUsuario;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Reservas.Tests.Reservas.Aplicacion.QueryHandlers
{
    public class QueryHandler_ObtenerReservasPorUsuario_Tests
    {
        private readonly Mock<IReservaRepository> MockReservaRepository;
        private readonly Mock<ILog> MockLogger;
        private readonly ObtenerReservasPorUsuarioQueryHandler Handler;

        // --- DATOS ---
        private readonly Guid UsuarioId;
        private readonly ObtenerReservasPorUsuarioQuery QueryValida;

        public QueryHandler_ObtenerReservasPorUsuario_Tests()
        {
            MockReservaRepository = new Mock<IReservaRepository>();
            MockLogger = new Mock<ILog>();

            Handler = new ObtenerReservasPorUsuarioQueryHandler(
                MockReservaRepository.Object,
                MockLogger.Object);

            UsuarioId = Guid.NewGuid();
            QueryValida = new ObtenerReservasPorUsuarioQuery(UsuarioId);
        }

        #region Handle_UsuarioConReservas_DeberiaRetornarListaDeReservaUsuarioDTO()
        [Fact]
        public async Task Handle_UsuarioConReservas_DeberiaRetornarListaDeReservaUsuarioDTO()
        {
            // ARRANGE
            var reservas = new List<Reserva>
            {
                CrearReservaDePrueba(Guid.NewGuid(), UsuarioId, asientosCount: 2),
                CrearReservaDePrueba(Guid.NewGuid(), UsuarioId, asientosCount: 1)
            };

            MockReservaRepository
                .Setup(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservas);

            // ACT
            var resultado = await Handler.Handle(QueryValida, CancellationToken.None);

            // ASSERT
            Assert.NotNull(resultado);
            Assert.IsType<List<ReservaUsuarioDTO>>(resultado);
            Assert.Equal(reservas.Count, resultado.Count);

            for (int i = 0; i < reservas.Count; i++)
            {
                var rDom = reservas[i];
                var dto = resultado[i];

                Assert.Equal(rDom.Id, dto.ReservaId);
                Assert.Equal(rDom.EventId.Value, dto.EventId);
                Assert.Equal(rDom.ZonaEventoId.Value, dto.ZonaEventoId);
                Assert.Equal(rDom.UsuarioId.Value, dto.UsuarioId);
                Assert.Equal(rDom.Estado.ToString(), dto.Estado);
                Assert.Equal(rDom.CreadaEn, dto.CreadaEn);
                Assert.Equal(rDom.ExpiraEn, dto.ExpiraEn);
                Assert.Equal(rDom.PrecioTotal, dto.PrecioTotal);

                Assert.NotNull(dto.Asientos);
                Assert.Equal(rDom.Asientos.Count, dto.Asientos.Count);

                foreach (var asientoDom in rDom.Asientos)
                {
                    Assert.Contains(dto.Asientos, a =>
                        a.AsientoId == asientoDom.AsientoId.Value &&
                        a.PrecioUnitario == asientoDom.PrecioUnitario &&
                        a.Label == asientoDom.Label);
                }
            }

            MockReservaRepository.Verify(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_UsuarioSinReservas_DeberiaRetornarListaVacia_CuandoRepoRetornaListaVacia()
        [Fact]
        public async Task Handle_UsuarioSinReservas_DeberiaRetornarListaVacia_CuandoRepoRetornaListaVacia()
        {
            // ARRANGE
            MockReservaRepository
                .Setup(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Reserva>());

            // ACT
            var resultado = await Handler.Handle(QueryValida, CancellationToken.None);

            // ASSERT
            Assert.NotNull(resultado);
            Assert.Empty(resultado);

            MockReservaRepository.Verify(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_UsuarioSinReservas_DeberiaRetornarListaVacia_CuandoRepoRetornaNull()
        [Fact]
        public async Task Handle_UsuarioSinReservas_DeberiaRetornarListaVacia_CuandoRepoRetornaNull()
        {
            // ARRANGE
            MockReservaRepository
                .Setup(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<Reserva>)null!);

            // ACT
            var resultado = await Handler.Handle(QueryValida, CancellationToken.None);

            // ASSERT
            Assert.NotNull(resultado);
            Assert.Empty(resultado);

            MockReservaRepository.Verify(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_RepositoryFalla_DeberiaPropagarExcepcion()
        [Fact]
        public async Task Handle_RepositoryFalla_DeberiaPropagarExcepcion()
        {
            // ARRANGE
            var dbException = new InvalidOperationException("Fallo simulado en repositorio.");

            MockReservaRepository
                .Setup(r => r.ObtenerPorUsuarioAsync(
                    QueryValida.UsuarioId,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(dbException);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Handler.Handle(QueryValida, CancellationToken.None));

            Assert.Equal("Fallo simulado en repositorio.", ex.Message);
        }
        #endregion

        #region Constructor_DependenciasNull_DeberiaLanzarExcepcionesDeGuardia()
        [Fact]
        public void Constructor_ReservaRepositoryNull_DeberiaLanzarReservaRepositoryNullException()
        {
            Assert.Throws<ReservaRepositoryNullException>(
                () => new ObtenerReservasPorUsuarioQueryHandler(
                    null!,
                    MockLogger.Object));
        }

        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            Assert.Throws<LoggerNullException>(
                () => new ObtenerReservasPorUsuarioQueryHandler(
                    MockReservaRepository.Object,
                    null!));
        }
        #endregion

        // ------------------------------------------------------------
        // Helper: construir una Reserva de prueba consistente
        // ------------------------------------------------------------
        private Reserva CrearReservaDePrueba(Guid reservaId, Guid usuarioIdGuid, int asientosCount)
        {
            var eventId = new Id(Guid.NewGuid(), "Evento");
            var zonaId = new Id(Guid.NewGuid(), "ZonaEvento");
            var usuarioId = new Id(usuarioIdGuid, "Usuario");
            var asientoPrincipalId = new Id(Guid.NewGuid(), "AsientoPrincipal");

            var creadaEn = DateTime.UtcNow.AddMinutes(-5);
            var expiraEn = DateTime.UtcNow.AddMinutes(15);

            var reserva = Reserva.Rehidratar(
                reservaId,
                eventId,
                zonaId,
                asientoPrincipalId,
                usuarioId,
                Reserva.ReservaEstado.Hold,
                creadaEn,
                expiraEn,
                precioTotal: 0m
            );

            for (int i = 0; i < asientosCount; i++)
            {
                var asientoId = (i == 0)
                    ? asientoPrincipalId
                    : new Id(Guid.NewGuid(), $"Asiento-{i + 1}");

                reserva.AgregarAsientoDesdeDocumento(
                    Guid.NewGuid(),
                    asientoId,
                    precioUnitario: 40m,
                    label: $"S-{i + 1}"
                );
            }

            return reserva;
        }
    }
}
