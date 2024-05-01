using System.Globalization;
namespace Store.Checkout.Services;

internal class Program
{
    private static int Main(string[] args)
    {
      
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHostedService<BackgroundScrubber>();
    
        var app = builder.Build();

        app.MapGet("/scrub", () =>
        {
            string x = Math.PI.ToString();
            for (int i = 0; i < 1000; i++)
            {
                x = x + Random.Shared.Next(0, 10).ToString();
                if (i % 50 == 0)
                {
                    Scrubber.SanitizeData("Working...", 'X', CultureInfo.CurrentCulture);
                }
            }

            return Scrubber.SanitizeData($"PI is {x}", 'X', CultureInfo.CurrentCulture);
        });

        app.MapGet("/", () => "Hello World! V2");
        app.Run();

        return 0;
    }
}