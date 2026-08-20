using FluentValidation;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Services;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator() => Configure(this);

    internal static void Configure<T>(AbstractValidator<T> validator)
        where T : IServiceCommand
    {
        validator.RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        validator.RuleFor(command => command.Description).MaximumLength(2_000);
        validator.RuleFor(command => command.DurationMinutes).GreaterThan(0);
        validator.RuleFor(command => command.PriceType).IsInEnum();
        validator.RuleFor(command => command.BasePrice)
            .NotNull()
            .GreaterThanOrEqualTo(0)
            .When(command => command.PriceType is ServicePriceType.Fixed or ServicePriceType.StartingAt);
        validator.RuleFor(command => command.BasePrice)
            .Null()
            .When(command => command.PriceType is ServicePriceType.QuoteRequired or ServicePriceType.Hidden);
        validator.RuleFor(command => command.ChecklistItems).NotNull();
        validator.RuleForEach(command => command.ChecklistItems).ChildRules(item =>
        {
            item.RuleFor(checklistItem => checklistItem.Description).NotEmpty().MaximumLength(500);
        });
    }
}

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator() => CreateServiceCommandValidator.Configure(this);
}

public interface IServiceCommand
{
    string Name { get; }
    string? Description { get; }
    int DurationMinutes { get; }
    ServicePriceType PriceType { get; }
    decimal? BasePrice { get; }
    bool AllowOnlineBooking { get; }
    bool RequireBeforePhotos { get; }
    bool RequireAfterPhotos { get; }
    IReadOnlyList<ServiceChecklistItemInput> ChecklistItems { get; }
}
