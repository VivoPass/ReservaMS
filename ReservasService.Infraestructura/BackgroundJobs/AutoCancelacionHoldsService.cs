using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReservasService.Aplicacion.Commands.Reservas.CancelacionAutomatica;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace ReservasService.Infraestructura.BackgroundJobs
{
    public class AutoCancelacionHoldsService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILog _logger;

        public AutoCancelacionHoldsService(IServiceProvider serviceProvider, ILog logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Info("[AutoCancelación] Servicio iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    await mediator.Send(new CancelarHoldsExpiradosCommand(), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.Error("[AutoCancelación] Error en la ejecución programada.", ex);
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }

}
