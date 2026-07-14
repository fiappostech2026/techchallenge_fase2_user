using FCG.Usuario.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Infra.Mappings
{
    public class UsuarioMap : IEntityTypeConfiguration<UsuarioEntity>
    {
        public void Configure(EntityTypeBuilder<UsuarioEntity> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(x => x.Id)
                .HasName("PK_Usuarios");

            builder.Property(x => x.Nome)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.SenhaHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.DataCadastro)
                .IsRequired();

            builder.Property(x => x.Perfil)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => x.Email)
                .IsUnique();
        }
    }
}
