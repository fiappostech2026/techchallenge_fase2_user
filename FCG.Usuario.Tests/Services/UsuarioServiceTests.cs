using FCG.Usuario.Domain.DTOs;
using FCG.Usuario.Domain.Entities;
using FCG.Usuario.Domain.Interfaces;
using FCG.Usuario.Domain.Services;
using Moq;
using Xunit;

namespace FCG.Usuario.Tests.Services;

public sealed class UsuarioServiceTests
{
    private static CriarUsuarioDto NovoUsuario() => new() { Nome = "Maria", Email = "maria@fcg.com", Senha = "Senha@123" };

    [Fact]
    public async Task CriarUsuarioAsync_EmailNovo_PersisteERetornaId()
    {
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((UsuarioEntity?)null);
        var sut = new UsuarioService(repository.Object, Mock.Of<IJwtService>());

        var id = await sut.CriarUsuarioAsync(NovoUsuario());

        Assert.NotNull(id);
        Assert.NotEqual(Guid.Empty, id!.Value);
        repository.Verify(r => r.AdicionarAsync(It.Is<UsuarioEntity>(u => u.Id == id.Value && u.Email == "maria@fcg.com")), Times.Once);
        repository.Verify(r => r.SalvarAlteracoesAsync(), Times.Once);
    }

    [Fact]
    public async Task CriarUsuarioAsync_SenhaNuncaEhGuardadaEmTextoPuro()
    {
        UsuarioEntity? salvo = null;
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.AdicionarAsync(It.IsAny<UsuarioEntity>())).Callback<UsuarioEntity>(u => salvo = u);
        var sut = new UsuarioService(repository.Object, Mock.Of<IJwtService>());

        await sut.CriarUsuarioAsync(NovoUsuario());

        Assert.NotNull(salvo);
        Assert.NotEqual("Senha@123", salvo!.SenhaHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Senha@123", salvo.SenhaHash));
    }

    [Fact]
    public async Task CriarUsuarioAsync_EmailJaCadastrado_RetornaNull_SemPersistir()
    {
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.ObterPorEmailAsync("maria@fcg.com")).ReturnsAsync(new UsuarioEntity { Email = "maria@fcg.com" });
        var sut = new UsuarioService(repository.Object, Mock.Of<IJwtService>());

        var id = await sut.CriarUsuarioAsync(NovoUsuario());

        Assert.Null(id);
        repository.Verify(r => r.AdicionarAsync(It.IsAny<UsuarioEntity>()), Times.Never);
    }

    [Fact]
    public async Task CriarUsuarioAsync_DtoNulo_LancaArgumentNullException()
    {
        var sut = new UsuarioService(Mock.Of<IUsuarioRepository>(), Mock.Of<IJwtService>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CriarUsuarioAsync(null!));
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokenDoJwtService()
    {
        var usuario = new UsuarioEntity { Id = Guid.NewGuid(), Email = "maria@fcg.com", SenhaHash = BCrypt.Net.BCrypt.HashPassword("Senha@123") };
        var esperado = new LoginResponseDto { AccessToken = "token" };
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.ObterPorEmailAsync(usuario.Email)).ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GerarToken(usuario)).Returns(esperado);
        var sut = new UsuarioService(repository.Object, jwt.Object);

        var result = await sut.LoginAsync(new LoginDto { Email = usuario.Email, Senha = "Senha@123" });

        Assert.Same(esperado, result);
    }

    [Fact]
    public async Task LoginAsync_SenhaErrada_RetornaNull_SemGerarToken()
    {
        var usuario = new UsuarioEntity { Email = "maria@fcg.com", SenhaHash = BCrypt.Net.BCrypt.HashPassword("Senha@123") };
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.ObterPorEmailAsync(usuario.Email)).ReturnsAsync(usuario);
        var jwt = new Mock<IJwtService>();
        var sut = new UsuarioService(repository.Object, jwt.Object);

        var result = await sut.LoginAsync(new LoginDto { Email = usuario.Email, Senha = "Errada@123" });

        Assert.Null(result);
        jwt.Verify(j => j.GerarToken(It.IsAny<UsuarioEntity>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_RetornaNull()
    {
        var repository = new Mock<IUsuarioRepository>();
        repository.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((UsuarioEntity?)null);
        var sut = new UsuarioService(repository.Object, Mock.Of<IJwtService>());

        var result = await sut.LoginAsync(new LoginDto { Email = "ninguem@fcg.com", Senha = "Senha@123" });

        Assert.Null(result);
    }
}
