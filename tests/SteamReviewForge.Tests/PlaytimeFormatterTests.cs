using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class PlaytimeFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("12.34", "12.3")]
    [InlineData("12,3", "12.3")]
    [InlineData("-5", "0")]
    [InlineData("9999999999", "999999999")]
    [InlineData("42.5 hours", "42.5")]
    public void Normalize_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(expected, PlaytimeFormatter.Normalize(input));
    }
}
