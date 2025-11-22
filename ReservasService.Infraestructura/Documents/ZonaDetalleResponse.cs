using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Infraestructura.Documents
{
    public class ZonaDetalleResponse
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string Nombre { get; set; } = default!;
        public decimal Precio { get; set; }          // 👈 lo importante para Reservas
        public string Estado { get; set; } = default!;
        public int Capacidad { get; set; }
        // Si el endpoint devuelve más campos, los agregas aquí según el JSON
    }
}
