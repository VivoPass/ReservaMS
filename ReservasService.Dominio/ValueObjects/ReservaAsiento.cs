using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.ValueObjects
{
    public class ReservaAsiento
    {
        public Guid Id { get; private set; }
        public Id AsientoId { get; private set; }
        public decimal PrecioUnitario { get; private set; }
        public string Label { get; private set; } = null!;

        internal ReservaAsiento(Guid id, Id asientoId, decimal precioUnitario, string label)
        {
            Id = id;
            AsientoId = asientoId ?? throw new ArgumentNullException(nameof(asientoId));
            PrecioUnitario = precioUnitario;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }
    }
}
