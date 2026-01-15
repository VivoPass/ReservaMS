using log4net;
using MediatR;
using ReservasService.Aplicacion.DTOS;
using ReservasService.Dominio.Excepciones.Reserva;
using ReservasService.Dominio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReservasService.Dominio.Excepciones.Api;

namespace ReservasService.Aplicacion.Queries.Reservas.ObtenerTodas
{
    public class ObtenerTodasLasReservasQueryHandler
        : IRequestHandler<ObtenerTodasLasReservasQuery, List<ReservaUsuarioDTO>>
    {
        private readonly IReservaRepository ReservaRepository;
        private readonly ILog Logger;

        public ObtenerTodasLasReservasQueryHandler(
            IReservaRepository reservaRepository,
            ILog logger)
        {
            ReservaRepository = reservaRepository ?? throw new ReservaRepositoryNullException();
            Logger = logger ?? throw new LoggerNullException();
        }

        public async Task<List<ReservaUsuarioDTO>> Handle(
            ObtenerTodasLasReservasQuery request,
            CancellationToken cancellationToken)
        {
            Logger.Debug("[ObtenerTodasLasReservas] Inicio de consulta.");

            // 👇 Aquí llamamos a un método del repo que AÚN NO EXISTE.
            // Así que también te lo dejo listo abajo. 
            var reservas = await ReservaRepository.ObtenerTodasAsync(cancellationToken);

            if (reservas is null || reservas.Count == 0)
            {
                Logger.Warn("[ObtenerTodasLasReservas] No existen reservas registradas.");
                return new List<ReservaUsuarioDTO>();
            }

            Logger.Debug($"[ObtenerTodasLasReservas] Total encontradas: ");

            var lista = reservas.Select(r => new ReservaUsuarioDTO
            {
                ReservaId = r.Id,
                EventId = r.EventId.Value,
                ZonaEventoId = r.ZonaEventoId.Value,
                UsuarioId = r.UsuarioId.Value,
                Estado = r.Estado.ToString(),
                CreadaEn = r.CreadaEn,
                ExpiraEn = r.ExpiraEn,
                PrecioTotal = r.PrecioTotal,

                Asientos = r.Asientos.Select(a => new ReservaAsientoDTO
                {
                    AsientoId = a.AsientoId.Value,
                    PrecioUnitario = a.PrecioUnitario,
                    Label = a.Label
                }).ToList()
            }).ToList();

            Logger.Debug("[ObtenerTodasLasReservas] Mapeo a DTO finalizado.");

            return lista;
        }
    }
}
