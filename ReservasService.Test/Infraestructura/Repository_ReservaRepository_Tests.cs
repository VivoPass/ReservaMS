using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MongoDB.Driver;
using Moq;
using ReservasService.Dominio.Entidades;
using ReservasService.Dominio.Excepciones.Infraestructura;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;
using ReservasService.Infraestructura.Repositorios;
using Reservas.Infrastructure.Interfaces;
using Xunit;

namespace ReservasService.Tests.Infraestructura.Repositorios
{
    public class Repository_ReservaRepository_Tests
    {
        private readonly Mock<IMongoDatabase> MockMongoDb;
        private readonly Mock<IMongoCollection<ReservaDocument>> MockReservaCollection;
        private readonly Mock<ILog> MockLogger;
        private readonly Mock<IAuditoriaRepository> MockAuditoria;

        private readonly ReservaRepository Repository;

        // --- DATOS ---
        private readonly Guid TestEventId = Guid.NewGuid();
        private readonly Guid TestZonaId = Guid.NewGuid();
        private readonly Guid TestAsientoId = Guid.NewGuid();
        private readonly Guid TestUsuarioId = Guid.NewGuid();
        private readonly Guid TestReservaId = Guid.NewGuid();

        private readonly Reserva TestReserva;

        public Repository_ReservaRepository_Tests()
        {
            MockMongoDb = new Mock<IMongoDatabase>();
            MockReservaCollection = new Mock<IMongoCollection<ReservaDocument>>();
            MockLogger = new Mock<ILog>();
            MockAuditoria = new Mock<IAuditoriaRepository>();

            MockMongoDb
                .Setup(db => db.GetCollection<ReservaDocument>("reservas", It.IsAny<MongoCollectionSettings>()))
                .Returns(MockReservaCollection.Object);

            Repository = new ReservaRepository(
                MockMongoDb.Object,
                MockLogger.Object,
                MockAuditoria.Object);

            TestReserva = Reserva.Rehidratar(
                TestReservaId,
                new Id(TestEventId, "EventId"),
                new Id(TestZonaId, "ZonaEventoId"),
                new Id(TestAsientoId, "AsientoId"),
                new Id(TestUsuarioId, "UsuarioId"),
                Reserva.ReservaEstado.Hold,
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow.AddMinutes(10),
                150m
            );
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        #region Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            Assert.Throws<LoggerNullException>(() =>
                new ReservaRepository(MockMongoDb.Object, null!, MockAuditoria.Object));
        }
        #endregion

        #region Constructor_AuditoriaNull_DeberiaLanzarAuditoriaRepositoryNullException()
        [Fact]
        public void Constructor_AuditoriaNull_DeberiaLanzarAuditoriaRepositoryNullException()
        {
            Assert.Throws<AuditoriaRepositoryNullException>(() =>
                new ReservaRepository(MockMongoDb.Object, MockLogger.Object, null!));
        }
        #endregion

        // ============================================================
        // ExisteHoldActivoAsync
        // ============================================================
        #region ExisteHoldActivo_HoldExiste_DeberiaRetornarTrue()
        [Fact]
        public async Task ExisteHoldActivo_HoldExiste_DeberiaRetornarTrue()
        {
            MockReservaCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1L);

            var result = await Repository.ExisteHoldActivoAsync(
                TestEventId, TestZonaId, TestAsientoId, CancellationToken.None);

            Assert.True(result);
        }
        #endregion

        #region ExisteHoldActivo_NoExiste_DeberiaRetornarFalse()
        [Fact]
        public async Task ExisteHoldActivo_NoExiste_DeberiaRetornarFalse()
        {
            MockReservaCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0L);

            var result = await Repository.ExisteHoldActivoAsync(
                TestEventId, TestZonaId, TestAsientoId, CancellationToken.None);

            Assert.False(result);
        }
        #endregion

        #region ExisteHoldActivo_FalloMongo_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task ExisteHoldActivo_FalloMongo_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error de conteo simulado.");

            MockReservaCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.ExisteHoldActivoAsync(
                    TestEventId, TestZonaId, TestAsientoId, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // CrearAsync
        // ============================================================
        #region CrearAsync_InsertCorrecto_DeberiaInsertarYRegistrarAuditoria()
        [Fact]
        public async Task CrearAsync_InsertCorrecto_DeberiaInsertarYRegistrarAuditoria()
        {
            MockReservaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            MockAuditoria
                .Setup(a => a.InsertarAuditoriaReserva(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await Repository.CrearAsync(TestReserva, CancellationToken.None);

            MockReservaCollection.Verify(c => c.InsertOneAsync(
                    It.Is<ReservaDocument>(d => d.Id == TestReserva.Id),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockAuditoria.Verify(a => a.InsertarAuditoriaReserva(
                    TestReserva.Id.ToString(),
                    "INFO",
                    "RESERVA_CREADA",
                    It.Is<string>(m => m.Contains(TestReserva.UsuarioId.Value.ToString()))),
                Times.Once);
        }
        #endregion

        #region CrearAsync_FalloInsert_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task CrearAsync_FalloInsert_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error al insertar.");

            MockReservaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.CrearAsync(TestReserva, CancellationToken.None));

            MockAuditoria.Verify(a =>
                    a.InsertarAuditoriaReserva(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }
        #endregion

        #region CrearAsync_FalloAuditoria_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task CrearAsync_FalloAuditoria_DeberiaLanzarReservaRepositoryException()
        {
            MockReservaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            MockAuditoria
                .Setup(a => a.InsertarAuditoriaReserva(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Error auditoría"));

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.CrearAsync(TestReserva, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // ActualizarAsync
        // ============================================================
        #region Actualizar_Modifica_DeberiaRegistrarAuditoria()
        [Fact]
        public async Task Actualizar_Modifica_DeberiaRegistrarAuditoria()
        {
            var replaceMock = new Mock<ReplaceOneResult>();
            replaceMock.SetupGet(r => r.IsAcknowledged).Returns(true);
            replaceMock.SetupGet(r => r.ModifiedCount).Returns(1);

            MockReservaCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceMock.Object);

            MockAuditoria
                .Setup(a => a.InsertarAuditoriaReserva(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await Repository.ActualizarAsync(TestReserva, CancellationToken.None);

            MockAuditoria.Verify(a =>
                    a.InsertarAuditoriaReserva(
                        TestReserva.Id.ToString(),
                        "INFO",
                        "RESERVA_ACTUALIZADA",
                        It.IsAny<string>()),
                Times.Once);
        }
        #endregion

        #region Actualizar_NoModifica_NoDebeRegistrarAuditoria()
        [Fact]
        public async Task Actualizar_NoModifica_NoDebeRegistrarAuditoria()
        {
            var replaceMock = new Mock<ReplaceOneResult>();
            replaceMock.SetupGet(r => r.IsAcknowledged).Returns(true);
            replaceMock.SetupGet(r => r.ModifiedCount).Returns(0);

            MockReservaCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(replaceMock.Object);

            await Repository.ActualizarAsync(TestReserva, CancellationToken.None);

            MockAuditoria.Verify(a =>
                    a.InsertarAuditoriaReserva(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                Times.Never);
        }
        #endregion

        #region Actualizar_FalloMongo_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task Actualizar_FalloMongo_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error en ReplaceOne");

            MockReservaCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<ReservaDocument>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.ActualizarAsync(TestReserva, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // ObtenerPorIdAsync  (FindAsync -> cursor mock)
        // ============================================================
        #region ObtenerPorId_Existe_DeberiaRetornarReserva()
        [Fact]
        public async Task ObtenerPorId_Existe_DeberiaRetornarReserva()
        {
            var doc = CrearReservaDocument(TestReservaId, TestUsuarioId);
            var docs = new List<ReservaDocument> { doc };

            var cursorMock = BuildCursor(docs);

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);

            var result = await Repository.ObtenerPorIdAsync(TestReservaId, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(TestReservaId, result!.Id);
            Assert.Equal(TestUsuarioId, result.UsuarioId.Value);
        }
        #endregion

        #region ObtenerPorId_NoExiste_DeberiaRetornarNull()
        [Fact]
        public async Task ObtenerPorId_NoExiste_DeberiaRetornarNull()
        {
            var cursorMock = BuildEmptyCursor();

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);

            var result = await Repository.ObtenerPorIdAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.Null(result);
        }
        #endregion

        #region ObtenerPorId_FalloMongo_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task ObtenerPorId_FalloMongo_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error en FindAsync");

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.ObtenerPorIdAsync(TestReservaId, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // ObtenerHoldsExpiradosHastaAsync
        // ============================================================
        #region ObtenerHoldsExpirados_ConResultados_DeberiaRetornarLista()
        [Fact]
        public async Task ObtenerHoldsExpirados_ConResultados_DeberiaRetornarLista()
        {
            var docs = new List<ReservaDocument>
            {
                CrearReservaDocument(Guid.NewGuid(), TestUsuarioId),
                CrearReservaDocument(Guid.NewGuid(), TestUsuarioId)
            };

            var cursorMock = BuildCursor(docs);

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);

            var result = await Repository.ObtenerHoldsExpiradosHastaAsync(DateTime.UtcNow, CancellationToken.None);

            Assert.Equal(2, result.Count);
        }
        #endregion

        #region ObtenerHoldsExpirados_FalloMongo_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task ObtenerHoldsExpirados_FalloMongo_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error en FindAsync");

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.ObtenerHoldsExpiradosHastaAsync(DateTime.UtcNow, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // ObtenerPorUsuarioAsync
        // ============================================================
        #region ObtenerPorUsuario_ConReservas_DeberiaRetornarLista()
        [Fact]
        public async Task ObtenerPorUsuario_ConReservas_DeberiaRetornarLista()
        {
            var docs = new List<ReservaDocument>
            {
                CrearReservaDocument(Guid.NewGuid(), TestUsuarioId),
                CrearReservaDocument(Guid.NewGuid(), TestUsuarioId)
            };

            var cursorMock = BuildCursor(docs);

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);

            var result = await Repository.ObtenerPorUsuarioAsync(TestUsuarioId, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(TestUsuarioId, r.UsuarioId.Value));
        }
        #endregion

        #region ObtenerPorUsuario_SinReservas_DeberiaRetornarListaVacia()
        [Fact]
        public async Task ObtenerPorUsuario_SinReservas_DeberiaRetornarListaVacia()
        {
            var cursorMock = BuildEmptyCursor();

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursorMock.Object);

            var result = await Repository.ObtenerPorUsuarioAsync(TestUsuarioId, CancellationToken.None);

            Assert.Empty(result);
        }
        #endregion

        #region ObtenerPorUsuario_FalloMongo_DeberiaLanzarReservaRepositoryException()
        [Fact]
        public async Task ObtenerPorUsuario_FalloMongo_DeberiaLanzarReservaRepositoryException()
        {
            var mongoEx = new MongoException("Error en FindAsync usuario");

            MockReservaCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<ReservaDocument>>(),
                    It.IsAny<FindOptions<ReservaDocument, ReservaDocument>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            await Assert.ThrowsAsync<ReservaRepositoryException>(() =>
                Repository.ObtenerPorUsuarioAsync(TestUsuarioId, CancellationToken.None));
        }
        #endregion

        // ============================================================
        // HELPERS
        // ============================================================
        private ReservaDocument CrearReservaDocument(Guid reservaId, Guid usuarioId)
        {
            return new ReservaDocument
            {
                Id = reservaId,
                EventId = TestEventId,
                ZonaEventoId = TestZonaId,
                AsientoId = TestAsientoId,
                UsuarioId = usuarioId,
                Estado = (int)Reserva.ReservaEstado.Hold,
                CreadaEn = DateTime.UtcNow.AddMinutes(-10),
                ExpiraEn = DateTime.UtcNow.AddMinutes(5),
                PrecioTotal = 150m,
                Asientos = new List<ReservaAsientoDocument>
                {
                    new ReservaAsientoDocument
                    {
                        Id = Guid.NewGuid(),
                        AsientoId = TestAsientoId,
                        PrecioUnitario = 150m,
                        Label = "A-1"
                    }
                }
            };
        }

        private Mock<IAsyncCursor<ReservaDocument>> BuildCursor(IReadOnlyList<ReservaDocument> docs)
        {
            var cursor = new Mock<IAsyncCursor<ReservaDocument>>();

            cursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(true)
                  .Returns(false);

            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true)
                  .ReturnsAsync(false);

            cursor.SetupGet(c => c.Current).Returns(docs);

            return cursor;
        }

        private Mock<IAsyncCursor<ReservaDocument>> BuildEmptyCursor()
        {
            var cursor = new Mock<IAsyncCursor<ReservaDocument>>();

            cursor.Setup(c => c.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(false);

            cursor.Setup(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

            cursor.SetupGet(c => c.Current).Returns(Array.Empty<ReservaDocument>());

            return cursor;
        }
    }
}
