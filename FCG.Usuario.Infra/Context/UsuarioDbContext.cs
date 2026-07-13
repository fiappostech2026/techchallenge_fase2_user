using FCG.Usuario.Domain.Entities;
using FCG.Usuario.Infra.Mappings;
using Microsoft.EntityFrameworkCore;

namespace FCG.Usuario.Infra.Context
{
    public class UsuarioDbContext : DbContext
    {
        public UsuarioDbContext(DbContextOptions<UsuarioDbContext> options) : base(options)
        {
        }

        public DbSet<UsuarioEntity> Usuario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsuarioDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

    }
}
