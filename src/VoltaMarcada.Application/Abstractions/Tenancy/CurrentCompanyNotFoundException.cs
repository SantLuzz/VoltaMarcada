namespace VoltaMarcada.Application.Abstractions.Tenancy;

public sealed class CurrentCompanyNotFoundException()
    : InvalidOperationException("The authenticated user is not linked to a company.");
