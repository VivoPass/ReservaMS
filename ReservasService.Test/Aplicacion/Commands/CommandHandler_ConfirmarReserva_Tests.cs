using log4net;
using Moq;
using ReservasService.Aplicacion.Commands.Reservas.ConfirmarReserva;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.Excepciones.Reserva;
using ReservasService.Dominio.Interfaces;
using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Reservas.Tests.Reservas.Aplicacion.CommandHandlers
{
    public class CommandHandler_ConfirmarReserva_Tests
    {
        private readonly Mock<IReservaRepository> MockReservaRepo;
        private readonly Mock<IAsientosDisponibilidadService> MockAsientosService;
        private readonly Mock<ILog> MockLog;
        private readonly ConfirmarReservaCommandHandler Handler;

        // --- DATOS ---
        private readonly Guid ReservaIdValida;
        private readonly ConfirmarReservaCommand CommandValido;

        public CommandHandler_ConfirmarReserva_Tests()
        {
            MockReservaRepo = new Mock<IReservaRepository>();
            MockAsientosService = new Mock<IAsientosDisponibilidadService>();
            MockLog = new Mock<ILog>();

            Handler = new ConfirmarReservaCommandHandler(
                MockReservaRepo.Object,
                MockAsientosService.Object,
                MockLog.Object
            );

            ReservaIdValida = Guid.NewGuid();
            CommandValido = new ConfirmarReservaCommand(ReservaIdValida);
        }

        #region Handle_ReservaExiste_DeberiaConfirmarYRetornarTrue()
        [Fact]
        public async Task Handle_ReservaExiste_DeberiaConfirmarYRetornarTrue()
        {
            // ARRANGE
            var reservaMock = CrearReservaDePrueba(ReservaIdValida, asientosCount: 2);

            MockReservaRepo
                .Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservaMock);

            MockAsientosService
                .Setup(s => s.ActualizarEstadoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            MockReservaRepo
                .Setup(r => r.ActualizarAsync(reservaMock, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // ACT
            var result = await Handler.Handle(CommandValido, CancellationToken.None);

            // ASSERT
            Assert.True(result);

            MockReservaRepo.Verify(r => r.ObtenerPorIdAsync(
                    ReservaIdValida,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockAsientosService.Verify(s => s.ActualizarEstadoAsync(
                    reservaMock.EventId.Value,
                    reservaMock.ZonaEventoId.Value,
                    It.IsAny<Guid>(),
                    "ocupado",
                    It.IsAny<CancellationToken>()),
                Times.Exactly(reservaMock.Asientos.Count));

            MockReservaRepo.Verify(r => r.ActualizarAsync(
                    reservaMock,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_ReservaNoExiste_DeberiaLanzarReservaNotFoundException()
        [Fact]
        public async Task Handle_ReservaNoExiste_DeberiaLanzarReservaNotFoundException()
        {
            // ARRANGE
            MockReservaRepo
                .Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Reserva)null!);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ReservaNotFoundException>(
                () => Handler.Handle(CommandValido, CancellationToken.None)
            );

            MockReservaRepo.Verify(r => r.ObtenerPorIdAsync(
                    ReservaIdValida,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        #endregion

        #region Handle_ErrorActualizandoAsientos_DeberiaLanzarConfirmarReservaCommandHandlerException()
        [Fact]
        public async Task Handle_ErrorActualizandoAsientos_DeberiaLanzarConfirmarReservaCommandHandlerException()
        {
            // ARRANGE
            var reservaMock = CrearReservaDePrueba(ReservaIdValida, asientosCount: 1);

            MockReservaRepo
                .Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservaMock);

            var infraEx = new InvalidOperationException("Fallo MS Eventos");

            MockAsientosService
                .Setup(s => s.ActualizarEstadoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(infraEx);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ConfirmarReservaCommandHandlerException>(
                () => Handler.Handle(CommandValido, CancellationToken.None)
            );
        }
        #endregion

        #region Handle_ErrorPersistiendoReserva_DeberiaLanzarConfirmarReservaCommandHandlerException()
        [Fact]
        public async Task Handle_ErrorPersistiendoReserva_DeberiaLanzarConfirmarReservaCommandHandlerException()
        {
            // ARRANGE
            var reservaMock = CrearReservaDePrueba(ReservaIdValida, asientosCount: 1);

            MockReservaRepo
                .Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(reservaMock);

            MockAsientosService
                .Setup(s => s.ActualizarEstadoAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var dbEx = new Exception("Fallo Mongo al actualizar");

            MockReservaRepo
                .Setup(r => r.ActualizarAsync(reservaMock, It.IsAny<CancellationToken>()))
                .ThrowsAsync(dbEx);

            // ACT & ASSERT
            await Assert.ThrowsAsync<ConfirmarReservaCommandHandlerException>(
                () => Handler.Handle(CommandValido, CancellationToken.None)
            );
        }
        #endregion

        #region Constructor_NullDependencies_DeberiaLanzarExcepcionesDeGuardia()
        [Fact]
        public void Constructor_ReservaRepositoryNull_DeberiaLanzarReservaRepositoryNullException()
        {
            Assert.Throws<ReservaRepositoryNullException>(
                () => new ConfirmarReservaCommandHandler(
                    null!,
                    MockAsientosService.Object,
                    MockLog.Object));
        }

        [Fact]
        public void Constructor_AsientosServiceNull_DeberiaLanzarAsientosDisponibilidadServiceNullException()
        {
            Assert.Throws<AsientosDisponibilidadServiceNullException>(
                () => new ConfirmarReservaCommandHandler(
                    MockReservaRepo.Object,
                    null!,
                    MockLog.Object));
        }

        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            Assert.Throws<LoggerNullException>(
                () => new ConfirmarReservaCommandHandler(
                    MockReservaRepo.Object,
                    MockAsientosService.Object,
                    null!));
        }
        #endregion

        // ------------------------------------------------------------
        // Helper: construir una Reserva de prueba consistente
        // ------------------------------------------------------------
        private Reserva CrearReservaDePrueba(Guid reservaId, int asientosCount)
        {
            var eventId = new Id(Guid.NewGuid(), "Evento");
            var zonaId = new Id(Guid.NewGuid(), "ZonaEvento");
            var usuarioId = new Id(Guid.NewGuid(), "Usuario");
            var asientoPrincipalId = new Id(Guid.NewGuid(), "AsientoPrincipal");

            var creadaEn = DateTime.UtcNow.AddMinutes(-5);
            var expiraEn = DateTime.UtcNow.AddMinutes(10);

            // Rehidratamos la reserva como Hold y no expirada
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

            // Agregamos asientos reconstruidos
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
