using System.Text.Json;
using Microsoft.JSInterop;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public sealed class ReviewDraftStorageService
{
    private const string StorageKey =
        "steam-review-forge-draft-v1";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _js;

    public ReviewDraftStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<ReviewDraft?> LoadAsync()
    {
        var json = await _js.InvokeAsync<string?>(
            "reviewDraftStorage.get",
            StorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ReviewDraft>(
            json,
            JsonOptions);
    }

    public ValueTask SaveAsync(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var json = JsonSerializer.Serialize(
            draft,
            JsonOptions);

        return _js.InvokeVoidAsync(
            "reviewDraftStorage.set",
            StorageKey,
            json);
    }

    public ValueTask ClearAsync()
    {
        return _js.InvokeVoidAsync(
            "reviewDraftStorage.remove",
            StorageKey);
    }

    public ValueTask<bool> ConfirmResetAsync()
    {
        return _js.InvokeAsync<bool>(
            "reviewDraftStorage.confirmReset");
    }
}