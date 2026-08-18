using FluentValidation;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.CompanyOnboarding;

public sealed class CompleteCompanyOnboardingCommandValidator : AbstractValidator<CompleteCompanyOnboardingCommand>
{
    public CompleteCompanyOnboardingCommandValidator()
    {
        RuleFor(command => command.LegalName).NotEmpty();
        RuleFor(command => command.TradeName).NotEmpty();
        RuleFor(command => command.Document)
            .NotEmpty()
            .Must(BeAValidBrazilianDocument)
            .WithMessage("A valid Brazilian document is required.");
        RuleFor(command => command.Phone).NotEmpty();
        RuleFor(command => command.WhatsApp).NotEmpty();
        RuleFor(command => command.City).NotEmpty();
        RuleFor(command => command.State).NotEmpty();
    }

    private static bool BeAValidBrazilianDocument(string? document) =>
        BrazilianDocument.TryNormalize(document, out _);
}
