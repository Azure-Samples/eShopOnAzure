﻿using eShop.HybridApp.Services;
using eShop.WebAppComponents.Services;
using Microsoft.Extensions.Logging;

namespace eShop.HybridApp;

public static class MauiProgram
{
    // NOTE: Must have a trailing slash on base URLs to ensure the full BaseAddress URL is used to resolve relative URLs
    private static string MobileBffHost = "http://localhost:61632";
    internal static string MobileBffCatalogBaseUrl = $"{MobileBffHost}/catalog-api/";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        builder.Services.AddHttpClient<CatalogService>(o => o.BaseAddress = new(MobileBffCatalogBaseUrl));
        builder.Services.AddSingleton<IProductImageUrlProvider, ProductImageUrlProvider>();

        return builder.Build();
    }
}
