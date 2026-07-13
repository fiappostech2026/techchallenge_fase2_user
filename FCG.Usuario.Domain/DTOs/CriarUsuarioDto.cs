using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.DTOs
{
    public class CriarUsuarioDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
