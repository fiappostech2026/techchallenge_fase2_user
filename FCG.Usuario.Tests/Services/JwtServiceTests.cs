using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FCG.Usuario.Domain.Configurations;
using FCG.Usuario.Domain.Entities;
using FCG.Usuario.Domain.Enums;
using FCG.Usuario.Domain.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace FCG.Usuario.Tests.Services;

public sealed class JwtServiceTests
{
    // Chave fictícia, usada só em teste — a real vem do Secret do Kubernetes.
    private static readonly JwtSettings Settings = new()
    {
        SecretKey = "chave-de-teste-unitario-com-mais-de-32-caracteres",
        Issuer = "FCG.Catalog.Api",
        Audience = "FCG.Catalog.Client",
        ExpirationHours = 8
    };

    private static UsuarioEntity Usuario() => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Maria",
        Email = "maria@fcg.com",
        Perfil = PerfilEnum.Admin
    };

    [Fact]
    public void GerarToken_UsuarioValido_TokenContemClaimsDoUsuario()
    {
        var usuario = Usuario();
        var sut = new JwtService(Options.Create(Settings));

        var result = sut.GerarToken(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(usuario.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(usuario.Email, token.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Admin", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GerarToken_UsuarioValido_UsaIssuerAudienceEExpiracaoConfigurados()
    {
        var sut = new JwtService(Options.Create(Settings));

        var result = sut.GerarToken(Usuario());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(Settings.Issuer, token.Issuer);
        Assert.Contains(Settings.Audience, token.Audiences);
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddHours(7.9), DateTime.UtcNow.AddHours(8.1));
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(8 * 60, result.ExpiresIn);
    }

    [Fact]
    public void GerarToken_UsuarioNulo_LancaArgumentNullException()
    {
        var sut = new JwtService(Options.Create(Settings));

        Assert.Throws<ArgumentNullException>(() => sut.GerarToken(null!));
    }
}
