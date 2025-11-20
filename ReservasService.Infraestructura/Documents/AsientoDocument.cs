using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReservasService.Infraestructura.Documents
{
    public class AsientoRemotoDTO
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public int FilaIndex { get; set; }
        public int ColIndex { get; set; }
    }
}
