using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Identity.Web;

namespace eShop.WebhookClient.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddAuthenticationServices();

        // Application services
        builder.Services.AddOptions<WebhookClientOptions>().BindConfiguration(nameof(WebhookClientOptions));
        builder.Services.AddSingleton<HooksRepository>();

        // HTTP client registrations
        builder.Services.AddHttpClient<WebhooksClient>(o => o.BaseAddress = new("http://webhooks-api"))
            .AddAuthToken();
    }

    public static void AddAuthenticationServices(this IHostApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        // Add Authentication services
        services.AddAuthorization();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddMicrosoftIdentityWebApp(builder.Configuration);

        services.Configure<CookieAuthenticationOptions>(options =>
        {
            // Must be distinct from WebApp's cookie name, otherwise the two sites will interfere
            // with each other when both are on localhost (yes, even when they are on different ports)
            options.Cookie.Name = ".AspNetCore.WebHooksClientIdentity";
        });

        services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddCascadingAuthenticationState();
    }
}
