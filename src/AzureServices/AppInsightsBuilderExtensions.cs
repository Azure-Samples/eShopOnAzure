using System.Text.Json;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

public static class AppInsightsBuilderExtensions
{
    public static IResourceBuilder<ApplicationInsightsResource> AddApplicationInsights(this IDistributedApplicationBuilder builder, string name, string? connectionString = null)
    {
        var appInsights = new ApplicationInsightsResource(name, connectionString);
        return builder.AddResource(appInsights)
                        .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter =>
                        WriteAppInsightsResourceToManifest(jsonWriter, appInsights.GetConnectionString())));
    }

    static void WriteAppInsightsResourceToManifest(Utf8JsonWriter jsonWriter, string? connectionString)
    {
        jsonWriter.WriteString("type", "azure.appinsights.v1");
        if (!string.IsNullOrEmpty(connectionString))
        {
            jsonWriter.WriteString("connectionString", connectionString);
        }
    }
}
