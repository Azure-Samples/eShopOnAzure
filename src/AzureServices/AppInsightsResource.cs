namespace Aspire.Hosting.Azure;

public sealed class ApplicationInsightsResource : Resource, IAzureResource, IResourceWithConnectionString
{
    private readonly string? _connectionString;

    public ApplicationInsightsResource(string name, string? connectionString)
        : base(name)
    {
        _connectionString = connectionString;
    }

    public string? GetConnectionString() => _connectionString;
}
