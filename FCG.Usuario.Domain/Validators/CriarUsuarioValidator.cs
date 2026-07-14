using FCG.Usuario.Domain.DTOs;
using FluentValidation;

namespace FCG.Usuario.Domain.Validators;

public class CriarUsuarioValidator : AbstractValidator<CriarUsuarioDto>
{
    public CriarUsuarioValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Matches("[A-Za-z]").WithMessage("A senha deve conter pelo menos uma letra.")
            .Matches("[0-9]").WithMessage("A senha deve conter pelo menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter pelo menos um caractere especial.");
    }
}
