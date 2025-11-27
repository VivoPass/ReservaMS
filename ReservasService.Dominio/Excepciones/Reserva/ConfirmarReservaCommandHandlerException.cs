using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Reserva
{
    public class ConfirmarReservaCommandHandlerException : Exception
    {
        public ConfirmarReservaCommandHandlerException(Exception inner)
            : base("Error inesperado en ConfirmarReservaCommandHandler.", inner) { }
    }
}
