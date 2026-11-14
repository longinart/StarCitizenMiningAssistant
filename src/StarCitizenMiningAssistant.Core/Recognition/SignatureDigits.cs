using System.Globalization;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Recognition;

/// <summary>
/// Turns one Windows OCR line into the signature digits, then keeps a number
/// only when two readings of the same frame agree. Decision 018.
/// </summary>
public static class SignatureDigits
{
    public static string? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var groups = text.Split(default(char[]?), StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new string(token.Where(char.IsDigit).ToArray()))
            .Where(digits => digits.Length > 0)
            .ToList();
        if (groups.Count == 0)
        {
            return null;
        }

        var longest = groups.OrderByDescending(group => group.Length).First();
        if (longest.Length >= 4)
        {
            return TrimZero(longest);
        }

        if (groups.Count == 2 && groups[1].Length == 3 && groups[0].Length is >= 1 and <= 3)
        {
            var joined = groups[0] + groups[1];
            return joined.Length >= 4 ? TrimZero(joined) : null;
        }

        return null;
    }

    public static string? Choose(string? first, string? second, MiningCatalog? catalog)
    {
        var left = Parse(first);
        var right = Parse(second);
        if (left is null)
        {
            return right;
        }

        if (right is null || left == right)
        {
            return left;
        }

        var longer = left.Length >= right.Length ? left : right;
        var shorter = left.Length >= right.Length ? right : left;
        if (longer.Length == shorter.Length + 1
            && longer[0] == '0'
            && longer.EndsWith(shorter, StringComparison.Ordinal))
        {
            return shorter;
        }

        if (shorter.Length >= 2
            && longer.Length - shorter.Length <= 3
            && (longer.StartsWith(shorter, StringComparison.Ordinal) || longer.EndsWith(shorter, StringComparison.Ordinal)))
        {
            return longer;
        }

        var leftKnown = Known(catalog, left);
        var rightKnown = Known(catalog, right);
        if (leftKnown != rightKnown && Close(left, right))
        {
            return leftKnown ? left : right;
        }

        return null;
    }

    private static string TrimZero(string digits) =>
        digits.Length > 4 && digits[0] == '0' ? digits.TrimStart('0') : digits;

    private static bool Known(MiningCatalog? catalog, string digits) =>
        catalog is not null
        && double.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
        && catalog.MatchSignature(number).Count > 0;

    private static bool Close(string left, string right)
    {
        if (Distance(left, right) > 1)
        {
            return false;
        }

        var prefix = 0;
        var limit = Math.Min(left.Length, right.Length);
        while (prefix < limit && left[prefix] == right[prefix])
        {
            prefix++;
        }

        var suffix = 0;
        while (suffix < limit && left[left.Length - 1 - suffix] == right[right.Length - 1 - suffix])
        {
            suffix++;
        }

        var shared = Math.Max(prefix, suffix);
        if (shared < 3)
        {
            return false;
        }

        var affix = prefix >= suffix ? left[..prefix] : left[^suffix..];
        return affix.Any(digit => digit != '0');
    }

    private static int Distance(string left, string right)
    {
        var row = new int[right.Length + 1];
        for (var column = 0; column <= right.Length; column++)
        {
            row[column] = column;
        }

        for (var y = 1; y <= left.Length; y++)
        {
            var previous = row[0];
            row[0] = y;
            for (var x = 1; x <= right.Length; x++)
            {
                var current = row[x];
                var cost = left[y - 1] == right[x - 1] ? 0 : 1;
                row[x] = Math.Min(Math.Min(row[x] + 1, row[x - 1] + 1), previous + cost);
                previous = current;
            }
        }

        return row[right.Length];
    }
}
