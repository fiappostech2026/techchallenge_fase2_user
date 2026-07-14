using FCG.Usuario.Domain.Entities;

namespace FCG.Usuario.Domain.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<UsuarioEntity?> ObterPorIdAsync(Guid id);
        Task<IEnumerable<UsuarioEntity>> ObterTodosAsync();
        Task<UsuarioEntity?> ObterPorEmailAsync(string email);
        Task AdicionarAsync(UsuarioEntity usuario);
        Task AtualizarAsync(UsuarioEntity usuario);
        Task ExcluirAsync(UsuarioEntity usuario);
        Task PromoverParaAdminAsync(UsuarioEntity usuario);
        Task SalvarAlteracoesAsync();
    }
}
