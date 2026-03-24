using CnpjCepValidation.Domain.ValueObjects;

namespace CnpjCepValidation.Application.Abstractions;

public interface IAddressComparer
{
    bool AreEqual(ComparableAddress a, ComparableAddress b);
}
