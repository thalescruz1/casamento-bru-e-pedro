using FluentValidation;

namespace Casamento.Application.Rsvps.Commands.SubmitRsvp;

public sealed class SubmitRsvpValidator : AbstractValidator<SubmitRsvpCommand>
{
    public SubmitRsvpValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Informe seu nome.")
            .MinimumLength(2).WithMessage("Nome muito curto.")
            .MaximumLength(120);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe um e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(254);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Informe um telefone.")
            .Must(HasAtLeastTenDigits).WithMessage("Telefone inválido.");

        RuleFor(x => x.Attend)
            .IsInEnum().WithMessage("Escolha uma opção.");

        RuleFor(x => x.Guests)
            .InclusiveBetween(0, 1).WithMessage("Acompanhantes: 0 ou 1.");

        When(x => x.Guests > 0, () =>
        {
            RuleFor(x => x.GuestNames)
                .NotEmpty().WithMessage("Informe o nome do acompanhante.")
                .MaximumLength(240);
        });

        RuleFor(x => x.Restrictions)
            .MaximumLength(500);

        RuleFor(x => x.IpHash)
            .NotEmpty();
    }

    private static bool HasAtLeastTenDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        var digits = value.Count(char.IsDigit);
        return digits is >= 10 and <= 13;
    }
}
