using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    /// <summary>
    /// Excepción lanzada cuando ocurre un error inesperado en el repositorio de Auditoría.
    /// </summary>
    public class AuditoriaRepositoryException : Exception
    {
        public AuditoriaRepositoryException()
            : base("Ocurrió un error en el repositorio de Auditoría.")
        {
        }

        public AuditoriaRepositoryException(string message)
            : base(message)
        {
        }

        public AuditoriaRepositoryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public AuditoriaRepositoryException(Exception innerException)
            : base("Ocurrió un error en el repositorio de Auditoría.", innerException)
        {
        }
    }
}
