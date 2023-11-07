using Microsoft.Identity.Web;

namespace WebhookClient;

internal static class Extensions
{
    public static IHostApplicationBuilder AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        // Add Authentication services
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApp(builder.Configuration);

        return builder;
    }

    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IWebhooksClient, WebhooksClient>(o => o.BaseAddress = new("http://webhooks-api")).AddAuthToken();
        builder.Services.AddSingleton<IHooksRepository, InMemoryHooksRepository>();

        builder.Services.AddOptions<WebhookClientOptions>()
            .BindConfiguration(nameof(WebhookClientOptions));
    }
}
