using log4net;
using Moq;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Aplicacion.Queries.Reservas.ObtenerReservaId;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Reservas.Tests.Reservas.Aplicacion.QueryHandlers
{
    public class QueryHandler_ObtenerReservasId_Tests
    {
        private readonly Mock<IReservaRepository> MockReservaRepository;
        private readonly Mock<ILog> MockLogger;
        private readonly ObtenerReservasIdQueryHandler Handler;

        // --- DATOS ---
        private readonly Guid ReservaIdExistente;
        private readonly ObtenerReservasIdQuery QueryValida;

        public QueryHandler_ObtenerReservasId_Tests()
        {
            MockReservaRepository = new Mock<IReservaRepository>();
            MockLogger = new Mock<ILog>();

            Handler = new ObtenerReservasIdQueryHandler(
                MockReservaRepository.Object,
                MockLogger.Object);

            ReservaIdExistente = Guid.NewGuid();
            QueryValida = new ObtenerReservasIdQuery(ReservaIdExistente);
        }

        #region Handle_ReservaExistente_DeberiaRetornarReservaUsuarioDTO()
        [Fact]
        public async Task Handle_ReservaExistente_DeberiaRetornarReservaUsuarioDTO()
        {
            // ARRANGE
            var reserva = CrearReservaDePrueba(ReservaIdExistente, asientosCount: 2);

            MockReservaRepository
                .Setup(r => r.ObtenerPorIdAsync(
                    QueryValida.ReservaGuid,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reserva);

            // ACT
            var resultDto = await Handler.Handle(QueryValida, CancellationToken.None);

            // ASSERT
            Assert.NotNull(resultDto);
            Assert.IsType<ReservaUsuarioDTO>(resultDto);

            Assert.Equal(reserva.Id, resultDto!.ReservaId);
            Assert.Equal(reserva.EventId.Value, resultDto.EventId);
            Assert.Equal(reserva.ZonaEventoId.Value, resultDto.ZonaEventoId);
            Assert.Equal(reserva.UsuarioId.Value, resultDto.UsuarioId);
            Assert.Equal(reserva.Estado.ToString(), resultDto.Estado);
            Assert.Equal(reserva.CreadaEn, resultDto.CreadaEn);
            Assert.Equal(reserva.ExpiraEn, resultDto.ExpiraEn);
            Assert.Equal(reserva.PrecioTotal, resultDto.PrecioTotal);

            Assert.NotNull(resultDto.Asientos);
            Assert.Equal(reserva.Asientos.Count, resultDto.Asientos.Count);

            foreach (var asientoDom in reserva.Asientos)
            {
                Assert.Contains(resultDto.Asientos, a =>
                    a.AsientoId == asientoDom.AsientoId.Value &&
                    a.PrecioUnitario == asientoDom.PrecioUnitario &&
                    a.Label == asientoDom.Label);
            }

            MockReservaRepository.Verify(r => r.ObtenerPorIdAsync(
                    QueryValida.ReservaGuid,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_ReservaNoExiste_DeberiaRetornarNull()
        [Fact]
        public async Task Handle_ReservaNoExiste_DeberiaRetornarNull()
        {
            // ARRANGE
            MockReservaRepository
                .Setup(r => r.ObtenerPorIdAsync(
                    QueryValida.ReservaGuid,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva)null!);

            // ACT
            var resultDto = await Handler.Handle(QueryValida, CancellationToken.None);

            // ASSERT
            Assert.Null(resultDto);

            MockReservaRepository.Verify(r => r.ObtenerPorIdAsync(
                    QueryValida.ReservaGuid,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_RepositoryFalla_DeberiaPropagarExcepcion()
        [Fact]
        public async Task Handle_RepositoryFalla_DeberiaPropagarExcepcion()
        {
            // ARRANGE
            var dbException = new InvalidOperationException("Fallo simulado en el repositorio.");

            MockReservaRepository
                .Setup(r => r.ObtenerPorIdAsync(
                    QueryValida.ReservaGuid,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(dbException);

            // ACT & ASSERT
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Handler.Handle(QueryValida, CancellationToken.None));

            Assert.Equal("Fallo simulado en el repositorio.", ex.Message);
        }
        #endregion

        #region Constructor_DependenciasNull_DeberiaLanzarExcepcionesDeGuardia()
        [Fact]
        public void Constructor_ReservaRepositoryNull_DeberiaLanzarReservaRepositoryNullException()
        {
            Assert.Throws<ReservaRepositoryNullException>(
                () => new ObtenerReservasIdQueryHandler(
                    null!,
                    MockLogger.Object));
        }

        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            Assert.Throws<LoggerNullException>(
                () => new ObtenerReservasIdQueryHandler(
                    MockReservaRepository.Object,
                    null!));
        }
        #endregion

        // Helper: construir una Reserva de prueba consistente
        private Reserva CrearReservaDePrueba(Guid reservaId, int asientosCount)
        {
            var eventId = new Id(Guid.NewGuid(), "Evento");
            var zonaId = new Id(Guid.NewGuid(), "ZonaEvento");
            var usuarioId = new Id(Guid.NewGuid(), "Usuario");
            var asientoPrincipalId = new Id(Guid.NewGuid(), "AsientoPrincipal");

            var creadaEn = DateTime.UtcNow.AddMinutes(-10);
            var expiraEn = DateTime.UtcNow.AddMinutes(20);

            var reserva = Reserva.Rehidratar(
                reservaId,
                eventId,
                zonaId,
                asientoPrincipalId,
                usuarioId,
                Reserva.ReservaEstado.Hold,
                creadaEn,
                expiraEn,
                precioTotal: 100m
            );

            for (int i = 0; i < asientosCount; i++)
            {
                var asientoId = (i == 0)
                    ? asientoPrincipalId
                    : new Id(Guid.NewGuid(), $"Asiento-{i + 1}");

                reserva.AgregarAsientoDesdeDocumento(
                    Guid.NewGuid(),
                    asientoId,
                    precioUnitario: 50m,
                    label: $"S-{i + 1}"
                );
            }

            return reserva;
        }
    }
}
