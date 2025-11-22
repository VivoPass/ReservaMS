using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Infraestructura.Documents
{
    public class ReservaAsientoDocument
    {
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Guid AsientoId { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Label { get; set; } = null!;
    }

}
