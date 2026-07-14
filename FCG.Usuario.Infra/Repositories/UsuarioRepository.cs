using FCG.Usuario.Domain.Entities;
using FCG.Usuario.Domain.Enums;
using FCG.Usuario.Domain.Interfaces;
using FCG.Usuario.Infra.Context;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FCG.Usuario.Infra.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly UsuarioDbContext _context;

        public UsuarioRepository(UsuarioDbContext context)
        {
            _context = context;
        }

        public async Task AdicionarAsync(UsuarioEntity usuario)
        {
            await _context.Usuario.AddAsync(usuario);
        }

        public async Task<IEnumerable<UsuarioEntity>> ObterTodosAsync()
        {
            return await _context.Usuario.ToListAsync();
        }

        public async Task<UsuarioEntity?> ObterPorEmailAsync(string email)
        {
            return await _context.Usuario.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UsuarioEntity?> ObterPorIdAsync(Guid id)
        {
            return await _context.Usuario.FindAsync(id);
        }

        public Task AtualizarAsync(UsuarioEntity usuario)
        {
            _context.Usuario.Update(usuario);
            return Task.CompletedTask;
        }

        public Task ExcluirAsync(UsuarioEntity usuario)
        {
            _context.Usuario.Remove(usuario);
            return Task.CompletedTask;
        }

        public Task PromoverParaAdminAsync(UsuarioEntity usuario)
        {
            usuario.Perfil = PerfilEnum.Admin;
            _context.Usuario.Update(usuario);
            return Task.CompletedTask;
        }

        public async Task SalvarAlteracoesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
