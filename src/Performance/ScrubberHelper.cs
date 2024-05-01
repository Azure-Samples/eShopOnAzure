using System.Globalization;
using static Store.Checkout.Services.Scrubber;

internal static class ScrubberHelpers
{

    public static IEnumerable<LocalizedWord> LoadDisallowedWords()
    {
        // Generate fake data. Could be loaded from resources, file on disk or external service.

        var cultures = new CultureInfo[]
        {
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("en-GB"),
            CultureInfo.GetCultureInfo("fr-FR"),
        };

        for (int i = 0; i < 3000; i++)
        {
            yield return new LocalizedWord(Text: DeserializeLocalizedTerm(), Culture: cultures[Random.Shared.Next(0, cultures.Length)]);
        }

        static string DeserializeLocalizedTerm()
        {
            Span<char> text = stackalloc char[Random.Shared.Next(4, 12)];
            for (int i = 0; i < text.Length; i++)
            {
                text[i] = (char)Random.Shared.Next('a', 'z' + 1);
            }

            return new string(text);
        }
    }
}
