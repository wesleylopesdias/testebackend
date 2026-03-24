namespace CnpjCepValidation.Domain.ValueObjects;

public sealed record Cnpj
{
    public string Value { get; }

    private Cnpj(string value) => Value = value;

    public static Cnpj Create(string input)
    {
        if (!TryValidate(input, out var digits, out var error))
            throw new ArgumentException(error, nameof(input));

        return new Cnpj(digits);
    }

    public static bool TryCreate(string input, out Cnpj? cnpj)
    {
        if (TryValidate(input, out var digits, out _))
        {
            cnpj = new Cnpj(digits);
            return true;
        }

        cnpj = null;
        return false;
    }

    private static bool TryValidate(string? input, out string digits, out string? errorMessage)
    {
        digits = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "CNPJ nao pode ser vazio.";
            return false;
        }

        digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length != 14)
        {
            errorMessage = "CNPJ deve ter 14 digitos.";
            return false;
        }

        if (digits.Distinct().Count() == 1)
        {
            errorMessage = "CNPJ com todos os digitos iguais e invalido.";
            return false;
        }

        if (!HasValidCheckDigits(digits))
        {
            errorMessage = "CNPJ com digitos verificadores invalidos.";
            return false;
        }

        return true;
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
