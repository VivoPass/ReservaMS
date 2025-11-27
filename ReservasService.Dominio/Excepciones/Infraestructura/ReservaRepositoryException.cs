using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    public class ReservaRepositoryException : Exception
    {
        public ReservaRepositoryException()
            : base("Ocurrió un error en el repositorio de Reservas.")
        {
        }

        public ReservaRepositoryException(string message)
            : base(message)
        {
        }

        public ReservaRepositoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ReservaRepositoryException(Exception innerException)
            : base("Ocurrió un error en el repositorio de Reservas.", innerException)
        {
        }
    }
}
