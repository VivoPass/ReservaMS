using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using log4net;

using ReservasService.Dominio.Interfacess;
using ReservasService.Dominio.ValueObjects;
using ReservasService.Infraestructura.Documents;
using ReservasService.Dominio.Excepciones.Infraestructura;

namespace ReservasService.Infraestructura.Repositorios
{
    /// <summary>
    /// Cliente HTTP contra EventsService para consultar y actualizar asientos.
    /// </summary>
    public class AsientosRepository : IAsientosDisponibilidadService
    {
        private readonly HttpClient _httpClient;
        private readonly ILog _logger;

        public AsientosRepository(HttpClient httpClient, ILog logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new LoggerNullException();
        }

        public async Task<List<AsientoDisponible>> ObtenerAsientosDisponiblesAsync(
            Guid eventId,
            Guid zonaEventoId,
            int cantidad,
            CancellationToken ct = default)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(cantidad));

            var urlAsientos = $"/api/events/{eventId}/zonas/{zonaEventoId}/asientos";
            var urlZona = $"/api/events/{eventId}/zonas/{zonaEventoId}?includeSeats=true";

            try
            {
                _logger.Info($"[Asientos] Consultando asientos disponibles. EventId='{eventId}', ZonaId='{zonaEventoId}', Cantidad={cantidad}.");
                _logger.Debug($"[Asientos] GET {urlAsientos}");

                // 1) Asientos de esa zona
                var asientos = await _httpClient
                    .GetFromJsonAsync<List<AsientoRemotoDTO>>(urlAsientos, ct)
                    .ConfigureAwait(false)
                    ?? new List<AsientoRemotoDTO>();

                _logger.Debug($"[Asientos] Recibidos {asientos.Count} asientos desde EventsService.");

                // 2) Info de la zona (para el precio)
                _logger.Debug($"[Asientos] GET {urlZona}");

                var zona = await _httpClient
                    .GetFromJsonAsync<ZonaDetalleResponse>(urlZona, ct)
                    .ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No se pudo obtener la zona del evento.");

                _logger.Debug($"[Asientos] Zona obtenida. Precio='{zona.Precio}'.");

                // 3) Filtrar disponibles, ordenar y tomar N
                var disponibles = asientos
                    .Where(a => string.Equals(a.Estado, "disponible", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(a => a.FilaIndex)
                    .ThenBy(a => a.ColIndex)
                    .Take(cantidad)
                    .ToList();

                _logger.Info($"[Asientos] Asientos disponibles seleccionados: {disponibles.Count} de {cantidad} solicitados.");

                // 4) Mapear al VO de dominio
                var resultado = disponibles
                    .Select(a => new AsientoDisponible(
                        a.Id,                 // AsientoId
                        zona.Precio,          // PrecioUnitario (de la zona)
                        a.Label ?? $"{a.FilaIndex}-{a.ColIndex}"
                    ))
                    .ToList();

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.Error($"[Asientos] Error al obtener asientos disponibles para EventId='{eventId}', ZonaId='{zonaEventoId}'.", ex);
                // Si quieres, puedes crear una excepción específica tipo AsientosDisponibilidadException.
                throw;
            }
        }

        public async Task ActualizarEstadoAsync(
            Guid eventId,
            Guid zonaEventoId,
            Guid asientoId,
            string nuevoEstado,
            CancellationToken ct = default)
        {
            var url = $"/api/events/{eventId}/zonas/{zonaEventoId}/asientos/{asientoId}";

            try
            {
                _logger.Info($"[Asientos] Actualizando estado de asiento. EventId='{eventId}', ZonaId='{zonaEventoId}', AsientoId='{asientoId}', NuevoEstado='{nuevoEstado}'.");
                _logger.Debug($"[Asientos] PUT {url}");

                var body = new ActualizarAsientoRequest
                {
                    Estado = nuevoEstado,
                    Label = null,
                    Meta = null
                };

                var response = await _httpClient.PutAsJsonAsync(url, body, ct);
                _logger.Debug($"[Asientos] Respuesta EventsService: {(int)response.StatusCode} {response.ReasonPhrase}.");

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.Error($"[Asientos] Error al actualizar estado de asiento '{asientoId}'.", ex);
                throw;
            }
        }
    }
}
