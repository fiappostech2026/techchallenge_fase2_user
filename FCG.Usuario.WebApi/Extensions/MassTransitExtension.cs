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
            var port = configuration.GetValue<int?>("RabbitMQ:Port");
            var useSsl = configuration.GetValue<bool>("RabbitMQ:UseSsl");

            services.AddMassTransit(x =>
            {
                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("users", false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(BuildHostUri(host, port, virtualHost, useSsl), h =>
                    {
                        h.Username(username);
                        h.Password(password);

                        if (useSsl)
                            h.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                    });

                    cfg.UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true);

                    cfg.Message<UserCreatedEvent>(msgCfg => msgCfg.SetEntityName("user-created-event"));

                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        // rabbitmqs:// (TLS) exigido pelo CloudAMQP; rabbitmq:// (sem TLS) segue funcionando local
        // com o RabbitMQ do docker-compose/k8s, sem porta/UseSsl configurados (comportamento antigo).
        private static Uri BuildHostUri(string host, int? port, string virtualHost, bool useSsl)
        {
            var scheme = useSsl ? "rabbitmqs" : "rabbitmq";
            var portSegment = port.HasValue ? $":{port}" : string.Empty;
            var vhostSegment = virtualHost == "/" ? string.Empty : $"/{Uri.EscapeDataString(virtualHost)}";

            return new Uri($"{scheme}://{host}{portSegment}{vhostSegment}");
        }
    }
}
