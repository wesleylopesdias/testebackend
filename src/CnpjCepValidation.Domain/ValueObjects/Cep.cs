namespace CnpjCepValidation.Domain.ValueObjects;

public sealed record Cep
{
    public string Value { get; }

    private Cep(string value) => Value = value;

    public static Cep Create(string input)
    {
        if (!TryValidate(input, out var digits, out var error))
            throw new ArgumentException(error, nameof(input));

        return new Cep(digits);
    }

    public static bool TryCreate(string input, out Cep? cep)
    {
        if (TryValidate(input, out var digits, out _))
        {
            cep = new Cep(digits);
            return true;
        }

        cep = null;
        return false;
    }

    private static bool TryValidate(string? input, out string digits, out string? errorMessage)
    {
        digits = string.Empty;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "CEP nao pode ser vazio.";
            return false;
        }

        digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length != 8)
        {
            errorMessage = "CEP deve ter 8 digitos.";
            return false;
        }

        return true;
    }

    public override string ToString() => Value;
}
