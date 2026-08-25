using System.Globalization;
using System.Text;

namespace VoltaMarcada.Application.CompanyOnboarding;

public static class CompanySlugGenerator
{
    public static string GenerateBaseSlug(string? value)
    {
        var normalizedValue = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(normalizedValue.Length);
        var previousWasSeparator = false;

        foreach (var character in normalizedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'A' and <= 'Z')
            {
                slug.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (slug.Length > 0 && !previousWasSeparator)
            {
                slug.Append('-');
                previousWasSeparator = true;
            }
        }

        return slug.ToString().Trim('-');
    }
}
