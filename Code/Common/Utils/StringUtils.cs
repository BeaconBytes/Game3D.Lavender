using System.Linq;

namespace Lavender.Common.Utils;

public static class StringUtils
{
    public static string Sanitize(string input, int maxLength = 16)
    {
        // Clamp the length
        if (input.Length > maxLength)
            input = input.Substring(0, maxLength);

        // Trim away \0 chars
        input = input.Trim('\0');
        
        // Only allow alpha-numeric digits AND '_'
        input = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return input;
    }
}