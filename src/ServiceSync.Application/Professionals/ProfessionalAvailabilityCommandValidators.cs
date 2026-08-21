using FluentValidation;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Professionals;

public sealed class SaveProfessionalAvailabilityCommandValidator : AbstractValidator<SaveProfessionalAvailabilityCommand>
{
    public SaveProfessionalAvailabilityCommandValidator()
    {
        RuleFor(command => command.WorkDays)
            .Must(workDays => workDays != ProfessionalWorkDays.None && (workDays & ~ProfessionalWorkDays.All) == 0)
            .WithMessage("At least one valid workday is required.");

        RuleFor(command => command)
            .Must(command =>
                command.EntryTime < command.LunchStartTime &&
                command.LunchStartTime < command.LunchEndTime &&
                command.LunchEndTime < command.ExitTime)
            .WithMessage("Work schedule times must follow entry, lunch start, lunch end, and exit order.");
    }
}

public sealed class CreateScheduleBlockCommandValidator : AbstractValidator<CreateScheduleBlockCommand>
{
    public CreateScheduleBlockCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.EndDate)
            .GreaterThanOrEqualTo(command => command.StartDate);
        RuleFor(command => command.Notes)
            .MaximumLength(ScheduleBlock.MaxNotesLength);
        RuleFor(command => command)
            .Must(HaveValidTimeRange)
            .WithMessage("Schedule block dates and times are invalid.");
    }

    private static bool HaveValidTimeRange(CreateScheduleBlockCommand command)
    {
        if (command.IsFullDay)
        {
            return true;
        }

        return command.StartDate == command.EndDate &&
               command.StartTime.HasValue &&
               command.EndTime.HasValue &&
               command.StartTime.Value < command.EndTime.Value;
    }
}
