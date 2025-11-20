namespace ReservasService.Api.Contracts
{
    public class ReservaHoldResponse
    {
        public Guid ReservaId { get; set; }
        public Guid AsientoId { get; set; }
        public DateTime ExpiraEn { get; set; }
    }
}
