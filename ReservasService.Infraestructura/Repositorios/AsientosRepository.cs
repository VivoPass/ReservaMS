using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ReservasService.Dominio.Interfacess;
using ReservasService.Infraestructura.Documents;

namespace ReservasService.Infraestructura.Repositorios
{
    public class AsientosRepository : IAsientosDisponibilidadService
    {
        private readonly HttpClient _httpClient;

        public AsientosRepository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Guid>> ObtenerAsientosDisponiblesAsync(
            Guid eventId,
            Guid zonaEventoId,
            int cantidad,
            CancellationToken ct = default)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(cantidad));

            // 1) Llamar al MS de Eventos: asientos de ESA zona
            var url = $"/api/eventos/{eventId}/zonas/{zonaEventoId}/asientos";

            var asientos = await _httpClient
                               .GetFromJsonAsync<List<AsientoRemotoDTO>>(url, ct)
                               .ConfigureAwait(false)
                           ?? new List<AsientoRemotoDTO>();

            // 2) Filtrar solo los "disponible", ordenar y tomar la cantidad
            var disponibles = asientos
                .Where(a => string.Equals(a.Estado, "disponible",
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.FilaIndex)
                .ThenBy(a => a.ColIndex)
                .Take(cantidad)
                .Select(a => a.Id)
                .ToList();

            return disponibles;
        }

        public async Task ActualizarEstadoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            string nuevoEstado,
            CancellationToken ct = default)
        {
            var url = $"/api/eventos/{eventId}/zonas/{zonaEventoId}/asientos/{asientoId}";

            var body = new ActualizarAsientoRequest
            {
                Estado = nuevoEstado,
                Label = null,
                Meta = null
            };

            var response = await _httpClient.PutAsJsonAsync(url, body, ct);

            response.EnsureSuccessStatusCode();
        }
    }
}