using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Reserva
{
    public class ReservaNotFoundException : Exception
    {
        public ReservaNotFoundException(Guid id)
            : base($"La reserva con ID '{id}' no existe.") { }
    }
}
