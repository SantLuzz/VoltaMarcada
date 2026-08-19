using FluentValidation;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.Customers;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Document)
            .NotEmpty()
            .Must(BeAValidBrazilianDocument)
            .WithMessage("A valid Brazilian document is required.");
        RuleFor(command => command.WhatsApp).NotEmpty();
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
