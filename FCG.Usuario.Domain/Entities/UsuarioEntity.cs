using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.Entities
{
    public class UsuarioEntity
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
    }
}
