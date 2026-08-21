using FluentValidation;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Professionals;

public sealed class SaveProfessionalAvailabilityCommandValidator : AbstractValidator<SaveProfessionalAvailabilityCommand>
{
    public SaveProfessionalAvailabilityCommandValidator()
    {
        RuleFor(command => command.DailySchedules)
            .NotNull()
            .NotEmpty()
            .Must(HaveUniqueValidWeekdays)
            .WithMessage("At least one daily schedule with a unique valid weekday is required.");

        RuleForEach(command => command.DailySchedules)
            .ChildRules(schedule =>
            {
                schedule.RuleFor(value => value.Weekday)
                    .Must(weekday => Enum.IsDefined(weekday))
                    .WithMessage("Daily schedule weekday is invalid.");
                schedule.RuleFor(value => value)
                    .Must(HaveValidTimeRange)
                    .WithMessage("Daily schedule times are invalid.");
            });
    }

    private static bool HaveUniqueValidWeekdays(IReadOnlyList<SaveProfessionalDailyScheduleCommand>? dailySchedules) =>
        dailySchedules is { Count: > 0 } &&
        dailySchedules.All(schedule => Enum.IsDefined(schedule.Weekday)) &&
        dailySchedules.Select(schedule => schedule.Weekday).Distinct().Count() == dailySchedules.Count;

    private static bool HaveValidTimeRange(SaveProfessionalDailyScheduleCommand schedule)
    {
        if (schedule.EntryTime >= schedule.ExitTime ||
            schedule.LunchStartTime.HasValue != schedule.LunchEndTime.HasValue)
        {
            return false;
        }

        return schedule.LunchStartTime is not { } lunchStartTime ||
               schedule.LunchEndTime is not { } lunchEndTime ||
               (schedule.EntryTime < lunchStartTime &&
                lunchStartTime < lunchEndTime &&
                lunchEndTime < schedule.ExitTime);
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
