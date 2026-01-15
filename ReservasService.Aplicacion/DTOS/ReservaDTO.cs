using ReservasService.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.DTOS
{
    public class ReservaDTO
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Guid ZonaEventoId { get; set; }
        public Guid UsuarioId { get; set; }

        public Guid AsientoPrincipalId { get; set; }

        public Reserva.ReservaEstado Estado { get; set; }

        public DateTime CreadaEn { get; set; }
        public DateTime? ExpiraEn { get; set; }

        public decimal PrecioTotal { get; set; }

        public List<ReservaAsientoDTO>? Asientos { get; set; }
    }
}
