using System.Diagnostics;
using System.Globalization;

namespace Store.Checkout.Services;

public class BackgroundScrubber : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan waitTime = TimeSpan.FromSeconds(5);
        TimeSpan burnTime = TimeSpan.FromSeconds(5);
        try
        {
            // Just wastes CPU
            while (true)
            {
                await Task.Delay(waitTime, stoppingToken);
                Stopwatch stopwatch = Stopwatch.StartNew();
                do
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    Scrubber.SanitizeData($"The secret word is {stopwatch.ElapsedMilliseconds}", '*', CultureInfo.InvariantCulture);
                } while (stopwatch.Elapsed < burnTime);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}