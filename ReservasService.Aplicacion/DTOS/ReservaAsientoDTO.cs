using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.DTOS
{
    public class ReservaAsientoDTO
    {
        public Guid AsientoId { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Label { get; set; } = null!;
    }
}
