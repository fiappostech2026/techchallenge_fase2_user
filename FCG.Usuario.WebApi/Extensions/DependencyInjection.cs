using FCG.Usuario.Domain.Configurations;
using FCG.Usuario.Domain.Interfaces;
using FCG.Usuario.Domain.Services;
using FCG.Usuario.Infra.Context;
using FCG.Usuario.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FCG.Usuario.WebApi.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UsuarioDbContext>(options =>
            {
                options.UseSqlite(
                    configuration.GetConnectionString("DefaultConnection"));
            });

            services.Configure<RabbitMqSettings>(
                    configuration.GetSection("RabbitMq"));

            services.Configure<JwtSettings>(
                    configuration.GetSection("Jwt"));

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IJwtService, JwtService>();

            return services;
        }
    }
}
