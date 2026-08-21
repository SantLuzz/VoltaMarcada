using FluentValidation;

namespace ServiceSync.Application.WorkOrders;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty(); RuleFor(x => x.EquipmentId).NotEmpty(); RuleFor(x => x.ServiceId).NotEmpty(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.ReportedProblem).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Items).NotNull().Must(items => items.Count <= 100);
        RuleForEach(x => x.Items).ChildRules(item =>
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
        RuleFor(x => x.CustomerId).NotEmpty(); RuleFor(x => x.EquipmentId).NotEmpty(); RuleFor(x => x.ServiceId).NotEmpty(); RuleFor(x => x.Priority).IsInEnum(); RuleFor(x => x.ReportedProblem).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Items).NotNull().Must(items => items.Count <= 100);
        RuleForEach(x => x.Items).ChildRules(item =>
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
