using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CnpjCepValidation.Domain.Services;

public static partial class AddressNormalizer
{
    [GeneratedRegex(@"[^a-zA-Z0-9 ]")]
    private static partial Regex NonAlphanumericPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesPattern();

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var recomposed = sb.ToString().Normalize(NormalizationForm.FormC);
        var noSpecial = NonAlphanumericPattern().Replace(recomposed, " ");
        var collapsed = MultipleSpacesPattern().Replace(noSpecial, " ").Trim();
        return collapsed.ToUpperInvariant();
    }
}
