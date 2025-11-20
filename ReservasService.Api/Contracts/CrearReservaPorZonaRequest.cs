namespace ReservasService.Api.Contracts
{
    public class CrearReservaPorZonaRequest
    {
        public Guid EventId { get; set; }
        public Guid ZonaEventoId { get; set; }
        public int CantidadBoletos { get; set; }
        public Guid UsuarioId { get; set; }
    }
}
