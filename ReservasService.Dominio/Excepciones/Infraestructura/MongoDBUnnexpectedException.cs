using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    /// <summary>
    /// Excepción lanzada cuando ocurre un error inesperado no relacionado directamente
    /// con la conexión a MongoDB, pero originado dentro del flujo de configuración o acceso.
    /// </summary>
    public class MongoDBUnnexpectedException : Exception
    {
        public MongoDBUnnexpectedException()
            : base("Ocurrió un error inesperado al interactuar con MongoDB.")
        {
        }

        public MongoDBUnnexpectedException(string message)
            : base(message)
        {
        }

        public MongoDBUnnexpectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MongoDBUnnexpectedException(Exception innerException)
            : base("Ocurrió un error inesperado al interactuar con MongoDB.", innerException)
        {
        }
    }

}
