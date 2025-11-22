using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.DTOS
{
    public class ReservaUsuarioDTO
    {
        public Guid ReservaId { get; set; }
        public Guid EventId { get; set; }
        public Guid ZonaEventoId { get; set; }
        public Guid UsuarioId { get; set; }

        public string Estado { get; set; } = null!;
        public DateTime CreadaEn { get; set; }
        public DateTime? ExpiraEn { get; set; }

        public decimal PrecioTotal { get; set; }

        public List<ReservaAsientoDTO> Asientos { get; set; } = new();
    }
}
