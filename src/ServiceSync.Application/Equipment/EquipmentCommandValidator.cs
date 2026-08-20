using FluentValidation;

namespace ServiceSync.Application.Equipment;

public sealed class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator() => Configure(this);

    internal static void Configure<T>(AbstractValidator<T> validator)
        where T : IEquipmentCommand
    {
        validator.RuleFor(command => command.CustomerId).NotEmpty();
        validator.RuleFor(command => command.Type).NotEmpty().MaximumLength(200);
        validator.RuleFor(command => command.Manufacturer).MaximumLength(200);
        validator.RuleFor(command => command.Model).MaximumLength(200);
        validator.RuleFor(command => command.SerialNumber).MaximumLength(200);
        validator.RuleFor(command => command.Capacity).MaximumLength(100);
        validator.RuleFor(command => command.LocationDescription).MaximumLength(500);
        validator.RuleFor(command => command.Notes).MaximumLength(2_000);
    }
}

public sealed class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator() => CreateEquipmentCommandValidator.Configure(this);
}

public interface IEquipmentCommand
{
    Guid CustomerId { get; }
    string Type { get; }
    string? Manufacturer { get; }
    string? Model { get; }
    string? SerialNumber { get; }
    string? Capacity { get; }
    string? LocationDescription { get; }
    DateOnly? InstallationDate { get; }
    string? Notes { get; }
}
