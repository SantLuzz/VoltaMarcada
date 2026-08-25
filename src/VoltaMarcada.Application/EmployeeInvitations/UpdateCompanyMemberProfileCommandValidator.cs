using FluentValidation;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.EmployeeInvitations;

public sealed class UpdateCompanyMemberProfileCommandValidator : AbstractValidator<UpdateCompanyMemberProfileCommand>
{
    public UpdateCompanyMemberProfileCommandValidator()
    {
        RuleFor(command => command.MembershipId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Document)
            .Must(IsValidCpf)
            .WithMessage("A valid CPF is required.");
        RuleFor(command => command.Phone).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Address).NotEmpty().MaximumLength(300);
    }

    private static bool IsValidCpf(string? value) =>
        BrazilianDocument.TryNormalize(value, out var normalizedDocument) && normalizedDocument.Length == 11;
}
