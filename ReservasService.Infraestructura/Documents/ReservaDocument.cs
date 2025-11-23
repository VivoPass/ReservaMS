using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Infraestructura.Documents
{
    public class ReservaDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Guid EventId { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Guid ZonaEventoId { get; set; }
        [BsonRepresentation(BsonType.String)]
        // lo dejamos por compatibilidad, será el “asiento principal”
        public Guid AsientoId { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Guid UsuarioId { get; set; }
        public int Estado { get; set; }
        public DateTime CreadaEn { get; set; }
        public DateTime? ExpiraEn { get; set; }

        public decimal PrecioTotal { get; set; }        // 👈 NUEVO

        public List<ReservaAsientoDocument> Asientos { get; set; } = new();  // 👈 NUEVO
    }
}
