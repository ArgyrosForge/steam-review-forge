using Microsoft.JSInterop;

namespace SteamReviewForge.Tests;

internal sealed class FakeJsRuntime : IJSRuntime
{
    public Dictionary<string, string> Storage { get; } = [];

    public bool ThrowOnAccess { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        object?[]? args)
    {
        return InvokeAsync<TValue>(
            identifier,
            CancellationToken.None,
            args);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        if (ThrowOnAccess)
        {
            throw new JSException("Storage is unavailable.");
        }

        var key = args?[0]?.ToString() ?? string.Empty;
        object? result = default(TValue);

        switch (identifier)
        {
            case "reviewDraftStorage.get":
                Storage.TryGetValue(key, out var value);
                result = value;
                break;

            case "reviewDraftStorage.set":
                Storage[key] = args?[1]?.ToString() ?? string.Empty;
                break;

            case "reviewDraftStorage.remove":
                Storage.Remove(key);
                break;

            default:
                throw new JSException($"Unexpected JavaScript call: {identifier}");
        }

        return ValueTask.FromResult((TValue?)result!);
    }
}
