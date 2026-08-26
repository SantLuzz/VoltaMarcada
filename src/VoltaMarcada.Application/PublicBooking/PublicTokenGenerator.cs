using System.Security.Cryptography;
using System.Text;

namespace VoltaMarcada.Application.PublicBooking;

public static class PublicTokenGenerator
{
    public static string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
