using log4net;
using Moq;
using ReservasService.Aplicacion.Commands.Reservas.CrearRerservaZona;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Reservas.Tests.Reservas.Aplicacion.CommandHandlers
{
    public class CommandHandler_CrearReservasPorZonaYCantidad_Tests
    {
        private readonly Mock<IReservaRepository> MockReservaRepo;
        private readonly Mock<IAsientosDisponibilidadService> MockAsientosService;
        private readonly Mock<ILog> MockLog;
        private readonly CrearReservasPorZonaYCantidadCommandHandler Handler;

        // --- DATOS ---
        private readonly Guid EventIdValido;
        private readonly Guid ZonaIdValida;
        private readonly Guid UsuarioIdValido;

        private readonly CrearReservasPorZonaYCantidadCommand CommandValido;

        public CommandHandler_CrearReservasPorZonaYCantidad_Tests()
        {
            MockReservaRepo = new Mock<IReservaRepository>();
            MockAsientosService = new Mock<IAsientosDisponibilidadService>();
            MockLog = new Mock<ILog>();

            Handler = new CrearReservasPorZonaYCantidadCommandHandler(
                MockReservaRepo.Object,
                MockAsientosService.Object,
                MockLog.Object);

            EventIdValido = Guid.NewGuid();
            ZonaIdValida = Guid.NewGuid();
            UsuarioIdValido = Guid.NewGuid();

            CommandValido = new CrearReservasPorZonaYCantidadCommand
            {
                EventId = EventIdValido,
                ZonaEventoId = ZonaIdValida,
                UsuarioId = UsuarioIdValido,
                CantidadBoletos = 2,
                TiempoHold = TimeSpan.FromMinutes(10)
            };
        }

        #region Handle_RequestValido_DeberiaCrearReservaYRetornarResultados()
        [Fact]
        public async Task Handle_RequestValido_DeberiaCrearReservaYRetornarResultados()
        {
            // ARRANGE
            var asientos = new List<AsientoDisponible>
            {
                new AsientoDisponible(Guid.NewGuid(), 50m, "A-1"),
                new AsientoDisponible(Guid.NewGuid(), 50m, "A-2")
            };

            MockAsientosService
                .Setup(s => s.ObtenerAsientosDisponiblesAsync(
                    EventIdValido,
                    ZonaIdValida,
                    CommandValido.CantidadBoletos,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asientos);

            MockReservaRepo
                .Setup(r => r.CrearAsync(
                    It.IsAny<ReservasService.Dominio.Entidades.Reserva>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            MockAsientosService
                .Setup(s => s.ActualizarEstadoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    "hold",
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // ACT
            var result = await Handler.Handle(CommandValido, CancellationToken.None);

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(CommandValido.CantidadBoletos, result.Count);

            MockAsientosService.Verify(s => s.ObtenerAsientosDisponiblesAsync(
                    EventIdValido,
                    ZonaIdValida,
                    CommandValido.CantidadBoletos,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockReservaRepo.Verify(r => r.CrearAsync(
                    It.IsAny<ReservasService.Dominio.Entidades.Reserva>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockAsientosService.Verify(s => s.ActualizarEstadoAsync(
                    EventIdValido,
                    ZonaIdValida,
                    It.IsAny<Guid>(),
                    "hold",
                    It.IsAny<CancellationToken>()),
                Times.Exactly(CommandValido.CantidadBoletos));
        }
        #endregion

        #region Handle_CantidadBoletosInvalida_DeberiaLanzarArgumentException()
        [Fact]
        public async Task Handle_CantidadBoletosInvalida_DeberiaLanzarArgumentException()
        {
            // ARRANGE
            var commandInvalido = new CrearReservasPorZonaYCantidadCommand
            {
                EventId = EventIdValido,
                ZonaEventoId = ZonaIdValida,
                UsuarioId = UsuarioIdValido,
                CantidadBoletos = 0,
                TiempoHold = TimeSpan.FromMinutes(10)
            };

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() =>
                Handler.Handle(commandInvalido, CancellationToken.None));
        }
        #endregion

        #region Handle_AsientosInsuficientes_DeberiaLanzarNoHayAsientosSuficientesException()
        [Fact]
        public async Task Handle_AsientosInsuficientes_DeberiaLanzarNoHayAsientosSuficientesException()
        {
            // ARRANGE
            var asientos = new List<AsientoDisponible>
            {
                new AsientoDisponible(Guid.NewGuid(), 50m, "A-1")
            };

            MockAsientosService
                .Setup(s => s.ObtenerAsientosDisponiblesAsync(
                    EventIdValido,
                    ZonaIdValida,
                    CommandValido.CantidadBoletos,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asientos);

            // ACT & ASSERT
            await Assert.ThrowsAsync<NoHayAsientosSuficientesException>(() =>
                Handler.Handle(CommandValido, CancellationToken.None));
        }
        #endregion

        #region Handle_ErrorInfraestructura_DeberiaLanzarCrearReservasPorZonaYCantidadCommandHandlerException()
        [Fact]
        public async Task Handle_ErrorInfraestructura_DeberiaLanzarCrearReservasPorZonaYCantidadCommandHandlerException()
        {
            // ARRANGE
            var asientos = new List<AsientoDisponible>
            {
                new AsientoDisponible(Guid.NewGuid(), 50m, "A-1"),
                new AsientoDisponible(Guid.NewGuid(), 50m, "A-2")
            };

            MockAsientosService
                .Setup(s => s.ObtenerAsientosDisponiblesAsync(
                    EventIdValido,
                    ZonaIdValida,
                    CommandValido.CantidadBoletos,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(asientos);

            var dbEx = new Exception("Error simulado en Mongo.");

            MockReservaRepo
                .Setup(r => r.CrearAsync(
                    It.IsAny<ReservasService.Dominio.Entidades.Reserva>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(dbEx);

            // ACT & ASSERT
            await Assert.ThrowsAsync<CrearReservasPorZonaYCantidadCommandHandlerException>(() =>
                Handler.Handle(CommandValido, CancellationToken.None));
        }
        #endregion

        #region Constructor_NullDependencies_DeberiaLanzarExcepcionesDeGuardia()
        [Fact]
        public void Constructor_ReservaRepositoryNull_DeberiaLanzarReservaRepositoryNullException()
        {
            Assert.Throws<ReservaRepositoryNullException>(
                () => new CrearReservasPorZonaYCantidadCommandHandler(
                    null!,
                    MockAsientosService.Object,
                    MockLog.Object));
        }

        [Fact]
        public void Constructor_AsientosServiceNull_DeberiaLanzarAsientosDisponibilidadServiceNullException()
        {
            Assert.Throws<AsientosDisponibilidadServiceNullException>(
                () => new CrearReservasPorZonaYCantidadCommandHandler(
                    MockReservaRepo.Object,
                    null!,
                    MockLog.Object));
        }

        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            Assert.Throws<LoggerNullException>(
                () => new CrearReservasPorZonaYCantidadCommandHandler(
                    MockReservaRepo.Object,
                    MockAsientosService.Object,
                    null!));
        }
        #endregion
    }
}
