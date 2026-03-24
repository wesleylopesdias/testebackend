using CnpjCepValidation.Application.Abstractions;
using CnpjCepValidation.Domain.Services;
using CnpjCepValidation.Domain.ValueObjects;

namespace CnpjCepValidation.Application.Services;

public sealed class AddressComparer : IAddressComparer
{
    public bool AreEqual(ComparableAddress a, ComparableAddress b) =>
        AddressNormalizer.Normalize(a.State) == AddressNormalizer.Normalize(b.State) &&
        AddressNormalizer.Normalize(a.City) == AddressNormalizer.Normalize(b.City) &&
        AddressNormalizer.Normalize(a.Street) == AddressNormalizer.Normalize(b.Street);
}
