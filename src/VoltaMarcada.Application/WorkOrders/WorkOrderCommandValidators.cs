using FluentValidation;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty(); RuleFor(x => x.EquipmentId).NotEmpty(); RuleFor(x => x.ServiceId).NotEmpty(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.ReportedProblem).MaximumLength(2000); RuleFor(x => x.ServiceUnitPrice).GreaterThanOrEqualTo(0).When(value => value.ServiceUnitPrice.HasValue); RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdditionalItems).NotNull().Must(items => items.Count <= 100);
        RuleForEach(x => x.AdditionalItems).ChildRules(item =>
        {
            item.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateWorkOrderCommandValidator : AbstractValidator<UpdateWorkOrderCommand>
{
    public UpdateWorkOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty(); RuleFor(x => x.EquipmentId).NotEmpty(); RuleFor(x => x.ServiceId).NotEmpty(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.ReportedProblem).MaximumLength(2000); RuleFor(x => x.ServiceUnitPrice).GreaterThanOrEqualTo(0).When(value => value.ServiceUnitPrice.HasValue); RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdditionalItems).NotNull().Must(items => items.Count <= 100);
        RuleForEach(x => x.AdditionalItems).ChildRules(item =>
        {
            item.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class UpdateWorkOrderAdditionalItemsCommandValidator : AbstractValidator<UpdateWorkOrderAdditionalItemsCommand>
{
    public UpdateWorkOrderAdditionalItemsCommandValidator()
    {
        RuleFor(x => x.AdditionalItems).NotNull().Must(items => items.Count <= 100);
        RuleForEach(x => x.AdditionalItems).ChildRules(item =>
        {
            item.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class SaveWorkOrderChecklistProgressCommandValidator : AbstractValidator<SaveWorkOrderChecklistProgressCommand>
{
    public SaveWorkOrderChecklistProgressCommandValidator()
    {
        RuleFor(x => x.ChecklistItems).NotNull().Must(items => items.Select(item => item.ChecklistItemId).Distinct().Count() == items.Count);
        RuleForEach(x => x.ChecklistItems).ChildRules(item => item.RuleFor(x => x.ChecklistItemId).NotEmpty());
        RuleFor(x => x.ExecutionNotes).MaximumLength(4000);
    }
}
