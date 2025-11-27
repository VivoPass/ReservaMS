using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.Excepciones.Reserva
{
    public class AsientosDisponibilidadServiceNullException : Exception
    {
        public AsientosDisponibilidadServiceNullException()
            : base("El IAsientosDisponibilidadService no fue inyectado.") { }
    }
}
