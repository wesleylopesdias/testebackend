using System.Net;
using System.Text;

namespace CnpjCepValidation.Integration.Fixtures;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string Pattern, Func<CancellationToken, Task<HttpResponseMessage>> Factory)> _mappings = [];

    public void When(string urlContains, Func<HttpResponseMessage> factory) =>
        _mappings.Add((urlContains, _ => Task.FromResult(factory())));

    public void WhenAsync(
        string urlContains,
        Func<CancellationToken, Task<HttpResponseMessage>> factory) =>
        _mappings.Add((urlContains, factory));

    public void WhenDelayed(
        string urlContains,
        TimeSpan delay,
        HttpStatusCode status,
        string? json = null)
    {
        _mappings.Add((urlContains, async cancellationToken =>
        {
            await Task.Delay(delay, cancellationToken);
            return CreateResponse(status, json);
        }));
    }

    public void When(string urlContains, HttpStatusCode status, string? json = null) =>
        _mappings.Add((urlContains, _ => Task.FromResult(CreateResponse(status, json))));

    public void Clear() => _mappings.Clear();

    public int CallCount { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CallCount++;
        var url = request.RequestUri?.ToString() ?? string.Empty;

        foreach (var (pattern, factory) in _mappings)
        {
            if (url.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return await factory(cancellationToken);
            }
        }

        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode status, string? json)
    {
        var response = new HttpResponseMessage(status);
        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
