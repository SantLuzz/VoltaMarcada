using FluentValidation;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.Customers;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(command => command.Document)
            .NotEmpty()
            .Must(BeAValidBrazilianDocument)
            .WithMessage("A valid Brazilian document is required.");
        RuleFor(command => command.WhatsApp)
            .NotEmpty()
            .MaximumLength(20);
        RuleFor(command => command.Email).MaximumLength(320);
        RuleFor(command => command.Email)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));
        RuleFor(command => command.Notes).MaximumLength(2_000);
        RuleFor(command => command.Addresses)
            .NotEmpty()
            .Must(HaveExactlyOnePrimaryAddress)
            .WithMessage("Exactly one primary address is required.");
        RuleForEach(command => command.Addresses).SetValidator(new CustomerAddressInputValidator());
    }

    private static bool BeAValidBrazilianDocument(string? document) =>
        BrazilianDocument.TryNormalize(document, out _);

    private static bool HaveExactlyOnePrimaryAddress(IReadOnlyCollection<CustomerAddressInput>? addresses) =>
        addresses?.Count(address => address.IsPrimary) == 1;
}

internal sealed class CustomerAddressInputValidator : AbstractValidator<CustomerAddressInput>
{
    public CustomerAddressInputValidator()
    {
        RuleFor(address => address.AddressType).IsInEnum();
        RuleFor(address => address.Street)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(address => address.Number)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(address => address.PostalCode)
            .NotEmpty()
            .Must(BeAValidBrazilianPostalCode)
            .WithMessage("A valid Brazilian postal code is required.");
        RuleFor(address => address.StateId).NotEmpty();
        RuleFor(address => address.CityId).NotEmpty();
        RuleFor(address => address.Complement).MaximumLength(200);
    }

    private static bool BeAValidBrazilianPostalCode(string? postalCode) =>
        BrazilianPostalCode.TryNormalize(postalCode, out _);
}
