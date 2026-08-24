namespace ServiceSync.Application.Abstractions.Communication;

public interface IWhatsAppLinkBuilder
{
    bool TryBuild(string phoneNumber, string message, out string link);
}
