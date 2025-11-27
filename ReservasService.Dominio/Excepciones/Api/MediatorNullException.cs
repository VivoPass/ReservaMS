using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Api
{
    public class MediatorNullException : Exception
    {
        public MediatorNullException()
            : base("IMediator no puede ser nulo al construir el controlador de Reservas.") { }
    }

    public class HttpClientFactoryNullException : Exception
    {
        public HttpClientFactoryNullException()
            : base("IHttpClientFactory no puede ser nulo al construir el controlador de Reservas.") { }
    }

    public class LoggerNullException : Exception
    {
        public LoggerNullException()
            : base("ILog no puede ser nulo al construir el controlador de Reservas.") { }
    }
}
