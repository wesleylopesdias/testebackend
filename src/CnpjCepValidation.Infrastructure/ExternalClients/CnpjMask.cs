namespace CnpjCepValidation.Infrastructure.ExternalClients;

internal static class CnpjMask
{
    public static string Apply(string cnpj)
    {
        if (cnpj.Length != 14) return "***";
        return $"{cnpj[..2]}.{cnpj[2..5]}.***/{cnpj[8..12]}-**";
    }
}
