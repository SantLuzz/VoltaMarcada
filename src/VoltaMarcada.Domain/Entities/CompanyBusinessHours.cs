using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class CompanyBusinessHours
{
    private CompanyBusinessHours()
    {
    }

    public Guid CompanyId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public bool IsOpen { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public TimeOnly? EndTime { get; private set; }

    internal static CompanyBusinessHours Create(Guid companyId, DayOfWeek dayOfWeek, bool isOpen, TimeOnly? startTime, TimeOnly? endTime)
    {
        var businessHours = new CompanyBusinessHours { CompanyId = companyId, DayOfWeek = dayOfWeek };
        businessHours.Update(isOpen, startTime, endTime);
        return businessHours;
    }

    internal void Update(bool isOpen, TimeOnly? startTime, TimeOnly? endTime)
    {
        if (isOpen && (startTime is null || endTime is null || endTime <= startTime))
        {
            throw new DomainException("Open business hours require a valid interval.");
        }

        IsOpen = isOpen;
        StartTime = isOpen ? startTime : null;
        EndTime = isOpen ? endTime : null;
    }
}

public sealed record CompanyBusinessHoursInput(DayOfWeek DayOfWeek, bool IsOpen, TimeOnly? StartTime, TimeOnly? EndTime);
