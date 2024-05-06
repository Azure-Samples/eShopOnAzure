using System.Diagnostics;
using System.Globalization;

namespace eShop.Store.Reviews;

public class BackgroundReviewValidation : BackgroundService
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
                    ReviewValidation.SanitizeData($"The secret word is {stopwatch.ElapsedMilliseconds}", '*', CultureInfo.InvariantCulture);
                } while (stopwatch.Elapsed < burnTime);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
