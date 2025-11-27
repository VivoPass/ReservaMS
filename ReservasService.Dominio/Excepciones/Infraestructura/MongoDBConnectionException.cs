using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    /// <summary>
    /// Excepción para errores relacionados con la conexión a MongoDB.
    /// </summary>
    public class MongoDBConnectionException : Exception
    {
        public MongoDBConnectionException()
            : base("Ocurrió un error al conectarse a MongoDB.")
        {
        }

        public MongoDBConnectionException(string message)
            : base(message)
        {
        }

        public MongoDBConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MongoDBConnectionException(Exception innerException)
            : base("Ocurrió un error al conectarse a MongoDB.", innerException)
        {
        }
    }
}
