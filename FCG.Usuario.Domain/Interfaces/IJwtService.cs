using FCG.Usuario.Domain.DTOs;
using FCG.Usuario.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.Interfaces
{
    public interface IJwtService
    {
        LoginResponseDto GerarToken(UsuarioEntity usuario);
    }
}
