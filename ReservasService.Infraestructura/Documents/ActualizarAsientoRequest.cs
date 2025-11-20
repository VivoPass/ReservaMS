using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Infraestructura.Documents
{
    public class ActualizarAsientoRequest
    {
        public string? Label { get; set; }   // lo puedes dejar null si no quieres cambiarlo
        public string Estado { get; set; } = null!;
        public object? Meta { get; set; }    // si no usas meta, lo dejas null
    }
}
