using FCG.Usuario.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.Interfaces
{
    public interface IUsuarioService
    {
        Task<Guid> CriarUsuarioAsync(CriarUsuarioDto dto);
        Task<LoginResponseDto> LoginAsync(LoginDto dto);
    }
}
