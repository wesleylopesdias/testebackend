namespace CnpjCepValidation.Domain.ValueObjects;

public sealed record Cep
{
    public string Value { get; }

    private Cep(string value) => Value = value;

    public static Cep Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input, nameof(input));

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length != 8)
            throw new ArgumentException("CEP deve ter 8 dígitos.", nameof(input));

        return new Cep(digits);
    }

    public static bool TryCreate(string input, out Cep? cep)
    {
        try
        {
            cep = Create(input);
            return true;
        }
        catch
        {
            cep = null;
            return false;
        }
    }

    public override string ToString() => Value;
}
