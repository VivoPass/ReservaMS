using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Microsoft.Extensions.Configuration;
using Reservas.Infrastructure.Configurations;
using Reservas.Infrastructure.Interfaces;
using Reservas.Infrastructure.Persistences.Repositories;
using ReservasService.Dominio.Excepciones.Infraestructura;
using Xunit;

namespace ReservasService.Tests.Infraestructura.Persistences.Repositories
{
    public class Repository_AuditoriaRepository_Tests
    {
        private readonly Mock<IMongoDatabase> MockMongoDb;
        private readonly Mock<IMongoCollection<BsonDocument>> MockAuditoriaCollection;
        private readonly Mock<ILog> MockLogger;

        private readonly AuditoriaRepository Repository;

        // --- DATOS ---
        private const string TestReservaId = "reserva_123";
        private const string TestReferenciaId = "operacion_456";
        private const string TestLevel = "INFO";
        private const string TestTipo = "TEST_TIPO";
        private const string TestMensaje = "Mensaje de prueba auditoría";

        public Repository_AuditoriaRepository_Tests()
        {
            MockMongoDb = new Mock<IMongoDatabase>();
            MockAuditoriaCollection = new Mock<IMongoCollection<BsonDocument>>();
            MockLogger = new Mock<ILog>();

            // Mock de configuración para AuditoriaDbConfig
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MongoDb:ConnectionString"])
                      .Returns("mongodb://localhost:27017");
            configMock.Setup(c => c["MongoDb:AuditoriaDatabaseName"])
                      .Returns("test_database");

            var mongoConfig = new AuditoriaDbConfig(configMock.Object);
            // Sobrescribimos la db para que use el mock
            mongoConfig.db = MockMongoDb.Object;

            MockMongoDb
                .Setup(d => d.GetCollection<BsonDocument>(
                    "auditoriaReservas",
                    It.IsAny<MongoCollectionSettings>()))
                .Returns(MockAuditoriaCollection.Object);

            Repository = new AuditoriaRepository(mongoConfig, MockLogger.Object);
        }

        // ============================================================
        // CONSTRUCTOR
        // ============================================================
        #region Constructor_LoggerNull_DeberiaLanzarLoggerNullException
        [Fact]
        public void Constructor_LoggerNull_DeberiaLanzarLoggerNullException()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MongoDb:ConnectionString"])
                      .Returns("mongodb://localhost:27017");
            configMock.Setup(c => c["MongoDb:AuditoriaDatabaseName"])
                      .Returns("test_database");

            var mongoConfig = new AuditoriaDbConfig(configMock.Object);

            // Act & Assert
            Assert.Throws<LoggerNullException>(() =>
                new AuditoriaRepository(mongoConfig, null!));
        }
        #endregion

        // ============================================================
        // InsertarAuditoriaReserva
        // ============================================================
        #region InsertarAuditoriaReserva_InvocacionExitosa_DebeLlamarInsertOneAsyncUnaVez
        [Fact]
        public async Task InsertarAuditoriaReserva_InvocacionExitosa_DebeLlamarInsertOneAsyncUnaVez()
        {
            // Arrange
            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await Repository.InsertarAuditoriaReserva(
                TestReservaId, TestLevel, TestTipo, TestMensaje);

            // Assert
            MockAuditoriaCollection.Verify(c => c.InsertOneAsync(
                    It.Is<BsonDocument>(doc =>
                        doc["idReserva"].AsString == TestReservaId &&
                        doc["level"].AsString == TestLevel &&
                        doc["tipo"].AsString == TestTipo &&
                        doc["mensaje"].AsString == TestMensaje),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockLogger.Verify(l => l.Debug(
                    It.Is<string>(s => s.Contains("Auditoría de Reserva insertada"))),
                Times.Once);
        }
        #endregion

        #region InsertarAuditoriaReserva_FalloMongo_DeberiaRethrowMongoException
        [Fact]
        public async Task InsertarAuditoriaReserva_FalloMongo_DeberiaRethrowMongoException()
        {
            // Arrange
            var mongoEx = new MongoException("Error de inserción simulado.");

            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() =>
                Repository.InsertarAuditoriaReserva(
                    TestReservaId, TestLevel, TestTipo, TestMensaje));

            MockLogger.Verify(l => l.Error(
                    It.Is<string>(s => s.Contains("[FATAL DB ERROR]")),
                    mongoEx),
                Times.Once);
        }
        #endregion

        #region InsertarAuditoriaReserva_ExceptionGenerica_DeberiaLanzarAuditoriaRepositoryException
        [Fact]
        public async Task InsertarAuditoriaReserva_ExceptionGenerica_DeberiaLanzarAuditoriaRepositoryException()
        {
            // Arrange
            var genericEx = new InvalidOperationException("Fallo inesperado");

            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(genericEx);

            // Act & Assert
            await Assert.ThrowsAsync<AuditoriaRepositoryException>(() =>
                Repository.InsertarAuditoriaReserva(
                    TestReservaId, TestLevel, TestTipo, TestMensaje));

            MockLogger.Verify(l => l.Fatal(
                    It.Is<string>(s => s.Contains("[FATAL ERROR] Excepción no controlada al insertar auditoría de reserva")),
                    genericEx),
                Times.Once);
        }
        #endregion

        // ============================================================
        // InsertarAuditoriaOperacion
        // ============================================================
        #region InsertarAuditoriaOperacion_InvocacionExitosa_DebeLlamarInsertOneAsyncUnaVez
        [Fact]
        public async Task InsertarAuditoriaOperacion_InvocacionExitosa_DebeLlamarInsertOneAsyncUnaVez()
        {
            // Arrange
            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await Repository.InsertarAuditoriaOperacion(
                TestReferenciaId, TestLevel, TestTipo, TestMensaje);

            // Assert
            MockAuditoriaCollection.Verify(c => c.InsertOneAsync(
                    It.Is<BsonDocument>(doc =>
                        doc["idReferencia"].AsString == TestReferenciaId &&
                        doc["level"].AsString == TestLevel &&
                        doc["tipo"].AsString == TestTipo &&
                        doc["mensaje"].AsString == TestMensaje),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            MockLogger.Verify(l => l.Debug(
                    It.Is<string>(s => s.Contains("Auditoría de operación insertada"))),
                Times.Once);
        }
        #endregion

        #region InsertarAuditoriaOperacion_FalloMongo_DeberiaRethrowMongoException
        [Fact]
        public async Task InsertarAuditoriaOperacion_FalloMongo_DeberiaRethrowMongoException()
        {
            // Arrange
            var mongoEx = new MongoException("Error de inserción operación simulado.");

            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(mongoEx);

            // Act & Assert
            await Assert.ThrowsAsync<MongoException>(() =>
                Repository.InsertarAuditoriaOperacion(
                    TestReferenciaId, TestLevel, TestTipo, TestMensaje));

            MockLogger.Verify(l => l.Error(
                    It.Is<string>(s => s.Contains("[FATAL DB ERROR] Fallo al insertar auditoría de operación")),
                    mongoEx),
                Times.Once);
        }
        #endregion

        #region InsertarAuditoriaOperacion_ExceptionGenerica_DeberiaLanzarAuditoriaRepositoryException
        [Fact]
        public async Task InsertarAuditoriaOperacion_ExceptionGenerica_DeberiaLanzarAuditoriaRepositoryException()
        {
            // Arrange
            var genericEx = new Exception("Excepción genérica de prueba");

            MockAuditoriaCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<BsonDocument>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(genericEx);

            // Act & Assert
            await Assert.ThrowsAsync<AuditoriaRepositoryException>(() =>
                Repository.InsertarAuditoriaOperacion(
                    TestReferenciaId, TestLevel, TestTipo, TestMensaje));

            MockLogger.Verify(l => l.Fatal(
                    It.Is<string>(s => s.Contains("[FATAL ERROR] Excepción no controlada al insertar auditoría de operación")),
                    genericEx),
                Times.Once);
        }
        #endregion
    }
}
