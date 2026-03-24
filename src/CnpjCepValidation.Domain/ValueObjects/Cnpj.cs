namespace CnpjCepValidation.Domain.ValueObjects;

public sealed record Cnpj
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input, nameof(input));

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length != 14)
            throw new ArgumentException("CNPJ deve ter 14 dígitos.", nameof(input));

        if (digits.Distinct().Count() == 1)
            throw new ArgumentException("CNPJ com todos os dígitos iguais é inválido.", nameof(input));

        if (!HasValidCheckDigits(digits))
            throw new ArgumentException("CNPJ com dígitos verificadores inválidos.", nameof(input));

        return new Cnpj(digits);
    }

    public static bool TryCreate(string input, out Cnpj? cnpj)
    {
        try
        {
            cnpj = Create(input);
            return true;
        }
        catch
        {
            cnpj = null;
            return false;
        }
    }

    private static bool HasValidCheckDigits(string digits)
    {
        int[] weights1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] weights2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        int sum1 = digits.Take(12).Select((d, i) => (d - '0') * weights1[i]).Sum();
        int rem1 = sum1 % 11;
        int check1 = rem1 < 2 ? 0 : 11 - rem1;

        if (digits[12] - '0' != check1) return false;

        int sum2 = digits.Take(13).Select((d, i) => (d - '0') * weights2[i]).Sum();
        int rem2 = sum2 % 11;
        int check2 = rem2 < 2 ? 0 : 11 - rem2;

        return digits[13] - '0' == check2;
    }

    public override string ToString() => Value;
}
