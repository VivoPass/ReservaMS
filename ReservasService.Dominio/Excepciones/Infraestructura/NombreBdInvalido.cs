using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    /// <summary>
    /// Excepción lanzada cuando el nombre de la base de datos no está definido 
    /// </summary>
    public class NombreBdInvalido : Exception
    {
        public NombreBdInvalido()
            : base("El nombre de la base de datos es inválido o no está configurado.")
        {
        }

        public NombreBdInvalido(string message)
            : base(message)
        {
        }

        public NombreBdInvalido(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
