using FluentValidation;

namespace ServiceSync.Application.Professionals;

public sealed class CreateProfessionalCommandValidator : AbstractValidator<CreateProfessionalCommand>
{
    public CreateProfessionalCommandValidator()
    {
        RuleFor(command => command.CompanyUserId).NotEmpty();
        RuleFor(command => command.ServiceIds).NotEmpty();
        RuleForEach(command => command.ServiceIds).NotEmpty();
        RuleFor(command => command.ServiceIds).Must(serviceIds => serviceIds.Distinct().Count() == serviceIds.Count)
            .WithMessage("Service IDs must be distinct.");
    }
}

public sealed class UpdateProfessionalCommandValidator : AbstractValidator<UpdateProfessionalCommand>
{
    public UpdateProfessionalCommandValidator()
    {
        RuleFor(command => command.CompanyUserId).NotEmpty();
        RuleFor(command => command.ServiceIds).NotEmpty();
        RuleForEach(command => command.ServiceIds).NotEmpty();
        RuleFor(command => command.ServiceIds).Must(serviceIds => serviceIds.Distinct().Count() == serviceIds.Count)
            .WithMessage("Service IDs must be distinct.");
    }
}
