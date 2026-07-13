using FCG.Usuario.Domain.Entities;

namespace FCG.Usuario.Domain.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<UsuarioEntity?> ObterPorEmailAsync(string email);
        Task<UsuarioEntity?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(UsuarioEntity usuario);
    }
}
