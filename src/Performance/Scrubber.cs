using System.Globalization;

namespace eShop.Store.Reviews;

public class ReviewValidation
{
    internal record LocalizedWord(string Text, CultureInfo Culture);

    private static IEnumerable<LocalizedWord> DisallowedWords { get; } = ScrubberHelpers.LoadDisallowedWords();

    public static string SanitizeData(string data, char replacementChar, CultureInfo culture)
    {
        List<string> wordList = DisallowedWords
            .Where(word => culture.Equals(CultureInfo.InvariantCulture) || culture.Equals(word.Culture))
            .Select(word => word.Text).ToList();

        foreach (string word in wordList)
        {
            data = data.Replace(word, replacementChar.ToString(), ignoreCase: true, culture);
        }   
        return data;
    }
}
