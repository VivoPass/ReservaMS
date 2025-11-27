using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Reserva
{
    public class ReservaRepositoryNullException : Exception
    {
        public ReservaRepositoryNullException()
            : base("El IReservaRepository no fue inyectado.") { }
    }
}
