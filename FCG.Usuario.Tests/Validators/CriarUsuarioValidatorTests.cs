using FCG.Usuario.Domain.DTOs;
using FCG.Usuario.Domain.Validators;
using Xunit;

namespace FCG.Usuario.Tests.Validators;

public sealed class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _sut = new();

    [Fact]
    public void Validate_DadosValidos_EhValido()
    {
        var result = _sut.Validate(new CriarUsuarioDto { Nome = "Maria", Email = "maria@fcg.com", Senha = "Senha@123" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("curta@1", "A senha deve ter no mínimo 8 caracteres.")]
    [InlineData("12345678@", "A senha deve conter pelo menos uma letra.")]
    [InlineData("SenhaSem@Num", "A senha deve conter pelo menos um número.")]
    [InlineData("Senha1234", "A senha deve conter pelo menos um caractere especial.")]
    public void Validate_SenhaFraca_RetornaMensagemEspecifica(string senha, string mensagem)
    {
        var result = _sut.Validate(new CriarUsuarioDto { Nome = "Maria", Email = "maria@fcg.com", Senha = senha });

        Assert.Contains(result.Errors, e => e.ErrorMessage == mensagem);
    }

    [Fact]
    public void Validate_EmailInvalido_EhInvalido()
    {
        var result = _sut.Validate(new CriarUsuarioDto { Nome = "Maria", Email = "nao-eh-email", Senha = "Senha@123" });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Formato de e-mail inválido.");
    }

    [Fact]
    public void Validate_NomeVazioOuLongoDemais_EhInvalido()
    {
        Assert.False(_sut.Validate(new CriarUsuarioDto { Nome = "", Email = "maria@fcg.com", Senha = "Senha@123" }).IsValid);
        Assert.False(_sut.Validate(new CriarUsuarioDto { Nome = new string('a', 151), Email = "maria@fcg.com", Senha = "Senha@123" }).IsValid);
    }
}
