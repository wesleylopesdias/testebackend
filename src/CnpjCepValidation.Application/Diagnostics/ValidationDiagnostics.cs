using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CnpjCepValidation.Application.Diagnostics;

public static class ValidationDiagnostics
{
    public const string ActivitySourceName = "CnpjCepValidation.Validation";
    public const string MeterName = "CnpjCepValidation.Validation";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> ValidationOutcomeCounter =
        Meter.CreateCounter<long>("validation.outcome.count");

    private static readonly Histogram<double> ValidationDurationHistogram =
        Meter.CreateHistogram<double>("validation.duration.ms", unit: "ms");

    private static readonly Counter<long> ExternalRetryCounter =
        Meter.CreateCounter<long>("validation.external.retry.count");

    private static readonly Counter<long> CepFallbackCounter =
        Meter.CreateCounter<long>("validation.cep.fallback.count");

    public static Activity? StartValidationActivity(string cnpj, string cep)
    {
        var activity = ActivitySource.StartActivity(
            "customer_registration_validation",
            ActivityKind.Internal);

        activity?.SetTag("validation.cnpj", cnpj);
        activity?.SetTag("validation.cep", cep);

        return activity;
    }

    public static void RecordOutcome(string reason)
    {
        ValidationOutcomeCounter.Add(
            1,
            new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordDuration(TimeSpan elapsed, string reason)
    {
        ValidationDurationHistogram.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordRetry(string dependency, int attempt)
    {
        ExternalRetryCounter.Add(
            1,
            new KeyValuePair<string, object?>("dependency", dependency),
            new KeyValuePair<string, object?>("attempt", attempt));
    }

    public static void RecordCepFallback(string fromProvider, string toProvider, string cause)
    {
        CepFallbackCounter.Add(
            1,
            new KeyValuePair<string, object?>("from_provider", fromProvider),
            new KeyValuePair<string, object?>("to_provider", toProvider),
            new KeyValuePair<string, object?>("cause", cause));
    }
}
