using System;

namespace ReservasService.Dominio.Excepciones.Infraestructura
{
    public class LoggerNullException : Exception
    {
        public LoggerNullException()
            : base("El logger (ILog) no puede ser nulo.")
        {
        }

        public LoggerNullException(string message)
            : base(message)
        {
        }

        public LoggerNullException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}