using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace eShop.Ordering.FunctionalTests;

public sealed class OrderingApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IHost _app;

    public IResourceBuilder<PostgresServerResource> Postgres { get; private set; }
    public IResourceBuilder<PostgresServerResource> IdentityDB { get; private set; }

    private string _postgresConnectionString;

    public OrderingApiFixture()
    {
        var options = new DistributedApplicationOptions { AssemblyName = typeof(OrderingApiFixture).Assembly.FullName, DisableDashboard = true };
        var appBuilder = DistributedApplication.CreateBuilder(options);
        Postgres = appBuilder.AddPostgres("OrderingDB");
        IdentityDB = appBuilder.AddPostgres("IdentityDB");
        _app = appBuilder.Build();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"ConnectionStrings:{Postgres.Resource.Name}", _postgresConnectionString }
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new AutoAuthorizeStartupFilter());

            var sbClient = Substitute.For<ServiceBusClient>();
            sbClient.CreateProcessor(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ServiceBusProcessorOptions>()).Returns(Substitute.For<ServiceBusProcessor>());

            services.AddSingleton(sbClient);
            services.AddSingleton(Substitute.For<ServiceBusAdministrationClient>());

            services.PostConfigure<JwtBearerOptions>("Bearer", o =>
            {
                o.Events.OnMessageReceived = m =>
                {
                    m.Principal = m.HttpContext.User;
                    m.Success();
                    return Task.CompletedTask;
                };
            });
        });
        return base.CreateHost(builder);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _app.Dispose();
        }
    }

    public async Task InitializeAsync()
    {
        await _app.StartAsync();
        _postgresConnectionString = await Postgres.Resource.GetConnectionStringAsync();
    }

    private class AutoAuthorizeStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<AutoAuthorizeMiddleware>();
                next(builder);
            };
        }
    }
}
