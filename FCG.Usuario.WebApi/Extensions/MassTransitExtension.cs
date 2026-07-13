using FCG.Usuario.Domain.Configurations;
using MassTransit;

namespace FCG.Usuario.WebApi.Extensions
{
    public static class MassTransitExtension
    {
        public static IServiceCollection ConfigureMassTransit(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var rabbitSettings = configuration
                .GetSection("RabbitMq")
                .Get<RabbitMqSettings>();

            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(
                        rabbitSettings!.Host,
                        rabbitSettings.VirtualHost,
                        h =>
                        {
                            h.Username(rabbitSettings.Username);
                            h.Password(rabbitSettings.Password);
                        });
                });
            });

            return services;
        }
    }
}
