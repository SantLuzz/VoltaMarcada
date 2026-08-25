using System.Security.Cryptography;

namespace VoltaMarcada.Application.EmployeeInvitations;

internal sealed record EmployeeInvitationToken(string Value, string Hash)
{
    public static EmployeeInvitationToken Create()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return new EmployeeInvitationToken(value, hash);
    }

    public static string CreateHash(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
