namespace SteamReviewForge.Services;

/// <summary>Shared, bounded scanning rules for Steam review markup.</summary>
public static class SteamBbCodeSyntax
{
    public const int MaximumCharacters = 50_000;
    public const int MaximumNesting = 64;
    public const int MaximumDiagnostics = 50;
    public const string SizeMessage = "Live preview and formatting checks are paused above 50,000 characters. Your full BBCode is still saved and can be copied.";

    public static bool TryGetLink(string target, out Uri? uri)
    {
        uri = null;
        target = target.Trim();
        if (string.IsNullOrWhiteSpace(target) || target.Any(char.IsControl))
            return false;

        // Steam's own reference allows [url=store.steampowered.com].
        // Never reinterpret an explicit non-web scheme as a hostname.
        if (!target.Contains("://", StringComparison.Ordinal))
        {
            var firstSegment = target.Split('/')[0];
            if (firstSegment.Contains(':'))
                return false;
            target = "https://" + target;
        }

        return Uri.TryCreate(target, UriKind.Absolute, out uri) &&
               uri.Scheme is "http" or "https" && !string.IsNullOrEmpty(uri.Host);
    }

    internal static IEnumerable<Token> Scan(string text)
    {
        var position = 0;
        while (position < text.Length)
        {
            var start = text.IndexOf('[', position);
            if (start < 0) yield break;
            var end = text.IndexOf(']', start + 1);
            if (end < 0) yield break;
            // Each character is visited at most a constant number of times,
            // even for input containing thousands of unmatched opening brackets.
            start = text.LastIndexOf('[', end, end - start + 1);
            position = end + 1;
            var body = text[(start + 1)..end];
            var closing = body.StartsWith('/');
            if (closing) body = body[1..];
            var nameEnd = 0;
            while (nameEnd < body.Length && char.IsAsciiLetterOrDigit(body[nameEnd])) nameEnd++;
            if (body == "*") nameEnd = 1;
            if (nameEnd == 0 || (body[0] != '*' && !char.IsAsciiLetter(body[0]))) continue;
            if (nameEnd < body.Length && body[nameEnd] is not ('=' or ' ')) continue;
            yield return new Token(start, end + 1, body[..nameEnd].ToLowerInvariant(), body[nameEnd..], closing);
        }
    }

    internal sealed record Token(int Start, int End, string Name, string Attributes, bool Closing);
}
