namespace LumaDX;

/// <summary>
/// String manipulation for shader loading
/// </summary>
public class String
{
    /// <summary>
    /// iterates through a string and replaces any occurrence of a string with another
    /// </summary>
    /// <param name="text">string to iterate through</param>
    /// <param name="stringToReplace">string to search for</param>
    /// <param name="newString">string to replace the search term with</param>
    /// <returns>new string where all occurrences of the search term have been replaced with a new string</returns>
    public static string ReplaceAll(string text, string stringToReplace, string newString)
    {
        int bufferLength = stringToReplace.Length;

        for (int i = 0; i < text.Length + 1 - bufferLength; i++)
        {
            if (text.Substring(i, bufferLength) == stringToReplace)
            {
                text = text.Substring(0, i) + newString +
                       text.Substring(i + bufferLength, text.Length - (i + bufferLength));

                i += newString.Length;
            }
        }

        return text;
    }
}