using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReservasService.Dominio.ValueObjects
{
    public class Id
    {
        public Guid Value { get; }
        public string _nombre { get; }

        public Id(Guid value, string nombre)
        {
            if (String.IsNullOrWhiteSpace(nombre)) throw new Exception("EL nombre no puede esar vacio");
            if (value == Guid.Empty)
                throw new ArgumentException("El GUID "+ nombre + " no puede ser vacío.", nameof(value));
            _nombre = nombre;
            Value = value;
        }

        // Factory helper (opcional)
        public static Id New(string nombre) => new Id(Guid.NewGuid(),nombre);

        public override string ToString() => Value.ToString();
    }
}
