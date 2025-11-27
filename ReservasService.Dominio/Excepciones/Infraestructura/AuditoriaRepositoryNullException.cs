using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    public class AuditoriaRepositoryNullException : Exception
    {
        public AuditoriaRepositoryNullException()
            : base("El repositorio de Auditoría no puede ser nulo.")
        {
        }

        public AuditoriaRepositoryNullException(string message)
            : base(message)
        {
        }

        public AuditoriaRepositoryNullException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
