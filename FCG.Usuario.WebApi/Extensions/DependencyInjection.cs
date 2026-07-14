using FCG.Usuario.Domain.Configurations;
using FCG.Usuario.Domain.Interfaces;
using FCG.Usuario.Domain.Services;
using FCG.Usuario.Domain.Validators;
using FCG.Usuario.Infra.Context;
using FCG.Usuario.Infra.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FCG.Usuario.WebApi.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection ConfigureDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<UsuarioDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"));
            });

            services.Configure<JwtSettings>(
                    configuration.GetSection("Jwt"));

            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IJwtService, JwtService>();

            services.AddValidatorsFromAssemblyContaining<CriarUsuarioValidator>();

            return services;
        }
    }
}
