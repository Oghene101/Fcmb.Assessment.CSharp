using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Fcmb.Assessment.CSharp.Api.Infrastructure.Observability;

internal static class OtlpLogExport
{
    private const string HttpProtobuf = "http/protobuf";

    extension(LoggerConfiguration loggerConfiguration)
    {
        // The sink's own endpoint defaults to localhost:4317, so it is handed the collector the trace exporter uses,
        // read from configuration (Aspire and the deployment set these); without one, logs stay on the console.
        public LoggerConfiguration WriteToOtlpCollector(IConfiguration configuration)
        {
            string? otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

            if (string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                return loggerConfiguration;
            }

            string? otlpProtocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

            string? otlpHeaders = configuration["OTEL_EXPORTER_OTLP_HEADERS"];

            bool overHttp = otlpProtocol == HttpProtobuf;

            string collectorEndpoint = otlpEndpoint.TrimEnd('/');

            // The variable names the collector; over HTTP each signal has its own path under it.
            string logsEndpoint = overHttp ? $"{collectorEndpoint}/v1/logs" : collectorEndpoint;

            OtlpProtocol protocol = overHttp ? OtlpProtocol.HttpProtobuf : OtlpProtocol.Grpc;

            Dictionary<string, string> headers = HeadersOf(otlpHeaders);

            loggerConfiguration.WriteTo.OpenTelemetry(sink =>
            {
                sink.Endpoint = logsEndpoint;
                sink.Protocol = protocol;
                sink.Headers = headers;
            });

            return loggerConfiguration;
        }
    }

    // "key1=value1,key2=value2" with URL-encoded values, as the OpenTelemetry specification defines the variable.
    private static Dictionary<string, string> HeadersOf(string? otlpHeaders)
    {
        string[] headerPairs = (otlpHeaders ?? string.Empty).Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return headerPairs
            .Select(headerPair => headerPair.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(nameAndValue => nameAndValue.Length == 2)
            .ToDictionary(nameAndValue => nameAndValue[0], nameAndValue => Uri.UnescapeDataString(nameAndValue[1]));
    }
}
