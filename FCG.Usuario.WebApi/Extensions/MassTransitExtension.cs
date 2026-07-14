using FCG.Usuario.Domain.Dto;
using MassTransit;

namespace FCG.Usuario.WebApi.Extensions
{
    public static class MassTransitExtension
    {
        public static IServiceCollection ConfigureMassTransit(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var host = configuration["RabbitMQ:Host"] ?? "localhost";
            var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
            var username = configuration["RabbitMQ:Username"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("users", false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(host, virtualHost, h =>
                    {
                        h.Username(username);
                        h.Password(password);
                    });

                    cfg.UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true);

                    cfg.Message<UserCreatedEvent>(msgCfg => msgCfg.SetEntityName("user-created-event"));

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
