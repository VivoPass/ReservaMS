using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    /// <summary>
    /// Excepción lanzada cuando la cadena de conexión de MongoDB no está configurada o es inválida.
    /// </summary>
    public class ConexionBdInvalida : Exception
    {
        public ConexionBdInvalida()
            : base("La cadena de conexión a la base de datos es inválida o no está configurada.")
        {
        }

        public ConexionBdInvalida(string message)
            : base(message)
        {
        }

        public ConexionBdInvalida(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
