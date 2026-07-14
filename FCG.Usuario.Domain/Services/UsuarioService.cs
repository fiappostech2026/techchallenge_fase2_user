using FCG.Usuario.Domain.DTOs;
using FCG.Usuario.Domain.Entities;
using FCG.Usuario.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IJwtService _jwtService;

        public UsuarioService(IUsuarioRepository usuarioRepository, IJwtService jwtService)
        {
            _usuarioRepository = usuarioRepository;
            _jwtService = jwtService;
        }

        public async Task<Guid?> CriarUsuarioAsync(CriarUsuarioDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var usuarioExistente = await _usuarioRepository.ObterPorEmailAsync(dto.Email);

            if (usuarioExistente != null)
                return null;

            var usuario = new UsuarioEntity
            {
                Id = Guid.NewGuid(),
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                DataCadastro = DateTime.UtcNow
            };

            await _usuarioRepository.AdicionarAsync(usuario);
            await _usuarioRepository.SalvarAlteracoesAsync();

            return usuario.Id;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var usuario = await _usuarioRepository.ObterPorEmailAsync(dto.Email);

            if (usuario == null)
                return null;

            var senhaValida = BCrypt.Net.BCrypt.Verify(dto.Senha, usuario.SenhaHash);

            if (!senhaValida)
                return null;

            return _jwtService.GerarToken(usuario);
        }
    }
}
