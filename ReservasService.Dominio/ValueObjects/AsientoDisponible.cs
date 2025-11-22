using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.ValueObjects
{
    public sealed record AsientoDisponible(Guid AsientoId, decimal PrecioUnitario, string label);
}
