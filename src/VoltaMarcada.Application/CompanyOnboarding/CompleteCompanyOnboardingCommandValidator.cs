using FluentValidation;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.CompanyOnboarding;

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
        RuleFor(command => command.WhatsApp).NotEmpty();
        RuleFor(command => command.PostalCode)
            .Must(BeAValidBrazilianPostalCode)
            .WithMessage("A valid Brazilian postal code is required.");
        RuleFor(command => command.Address).NotEmpty();
        RuleFor(command => command.StateId).NotEmpty();
        RuleFor(command => command.CityId).NotEmpty();
    }

    private static bool BeAValidBrazilianDocument(string? document) =>
        BrazilianDocument.TryNormalize(document, out _);

    private static bool BeAValidBrazilianPostalCode(string? postalCode) =>
        BrazilianPostalCode.TryNormalize(postalCode, out _);
}
