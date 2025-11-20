using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Aplicacion.DTOS
{
    public class ReservaHoldResultDTO
    {
        public Guid ReservaId { get; }
        public Guid AsientoId { get; }
        public DateTime ExpiraEn { get; }

        public ReservaHoldResultDTO(Guid reservaId, Guid asientoId, DateTime expiraEn)
        {
            ReservaId = reservaId;
            AsientoId = asientoId;
            ExpiraEn = expiraEn;
        }
    }
}
