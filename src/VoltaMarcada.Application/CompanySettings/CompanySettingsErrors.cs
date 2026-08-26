using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.CompanySettings;

public static class CompanySettingsErrors
{
    public static readonly OperationError Forbidden = new("CompanySettings.Forbidden", OperationErrorType.Forbidden, "Only company owners can update public settings.");
    public static readonly OperationError NotFound = new("CompanySettings.NotFound", OperationErrorType.NotFound, "Company was not found.");
    public static readonly OperationError InvalidInput = new("CompanySettings.InvalidInput", OperationErrorType.Validation, "Company public settings are invalid.");
}
