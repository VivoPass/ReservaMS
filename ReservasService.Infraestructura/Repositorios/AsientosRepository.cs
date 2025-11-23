using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;
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

        public async Task<List<AsientoDisponible>> ObtenerAsientosDisponiblesAsync(
            Guid eventId,
            Guid zonaEventoId,
            int cantidad,
            CancellationToken ct = default)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(cantidad));

            // 1) Asientos de ESA zona
            var urlAsientos = $"/api/eventos/{eventId}/zonas/{zonaEventoId}/asientos";

            var asientos = await _httpClient
                               .GetFromJsonAsync<List<AsientoRemotoDTO>>(urlAsientos, ct)
                               .ConfigureAwait(false)
                           ?? new List<AsientoRemotoDTO>();

            // 2) Info de la zona (para el precio)
            var urlZona = $"/api/eventos/{eventId}/zonas/{zonaEventoId}?includeSeats=true";

            var zona = await _httpClient
                           .GetFromJsonAsync<ZonaDetalleResponse>(urlZona, ct)
                           .ConfigureAwait(false)
                       ?? throw new InvalidOperationException("No se pudo obtener la zona del evento.");

            // 3) Filtrar disponibles, ordenar y tomar N
            var disponibles = asientos
                .Where(a => string.Equals(a.Estado, "disponible",
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.FilaIndex)
                .ThenBy(a => a.ColIndex)
                .Take(cantidad)
                .ToList();

            // 4) Mapear al VO de dominio
            var resultado = disponibles
                .Select(a => new AsientoDisponible(
                    a.Id,                 // AsientoId
                    zona.Precio,          // PrecioUnitario (de la zona)
                    a.Label ?? $"{a.FilaIndex}-{a.ColIndex}"  // Label (o algo derivado)
                ))
                .ToList();

            return resultado;
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