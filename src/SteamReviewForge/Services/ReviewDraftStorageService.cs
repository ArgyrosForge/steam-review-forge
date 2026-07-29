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

        var draft = JsonSerializer.Deserialize<ReviewDraft>(
            json,
            JsonOptions);

        using var document = JsonDocument.Parse(json);
        var hasTableColumns =
            document.RootElement.TryGetProperty(
                "tableColumns",
                out _);

        if (draft?.Recommendation is not null &&
            !Enum.IsDefined(draft.Recommendation.Value))
        {
            draft.Recommendation = null;
        }

        if (draft is not null)
        {
            if (!hasTableColumns)
            {
                draft.TableColumns =
                    CreateLegacyTableColumns(
                        document.RootElement);
            }

            NormalizeTableColumns(draft);

            if (!Enum.IsDefined(draft.RatingSystem))
            {
                draft.RatingSystem =
                    ReviewRatingSystem.FiveStars;
            }

            draft.TextRatingOptions ??=
            [
                "Terrible",
                "Bad",
                "Mixed",
                "Good",
                "Excellent"
            ];

            if (draft.TextRatingOptions.Count == 0)
            {
                draft.TextRatingOptions.Add("Rating");
            }

            var maximumRating =
                draft.RatingSystem switch
                {
                    ReviewRatingSystem.TenPoint => 10,
                    ReviewRatingSystem.Text =>
                        draft.TextRatingOptions.Count,
                    _ => 5
                };

            foreach (var category in
                     draft.Categories ?? [])
            {
                category.CustomCells ??= [];
                category.Rating = Math.Clamp(
                    category.Rating,
                    1,
                    maximumRating);
            }

            draft.Playtime =
                PlaytimeFormatter.Normalize(draft.Playtime);
        }

        return draft;
    }

    private static List<ReviewTableColumn>
        CreateLegacyTableColumns(JsonElement root)
    {
        var columns = new List<ReviewTableColumn>();

        if (GetLegacyColumnVisibility(
                root,
                "showCategoryColumn"))
        {
            columns.Add(
                CreateColumn(
                    "Category",
                    ReviewTableColumnKind.Category));
        }

        if (GetLegacyColumnVisibility(
                root,
                "showRatingColumn"))
        {
            columns.Add(
                CreateColumn(
                    "Rating",
                    ReviewTableColumnKind.Rating));
        }

        if (GetLegacyColumnVisibility(
                root,
                "showNoteColumn"))
        {
            columns.Add(
                CreateColumn(
                    "Notes",
                    ReviewTableColumnKind.Note));
        }

        if (columns.Count == 0)
        {
            columns.Add(
                CreateColumn(
                    "Category",
                    ReviewTableColumnKind.Category));
        }

        return columns;
    }

    private static bool GetLegacyColumnVisibility(
        JsonElement root,
        string propertyName)
    {
        return !root.TryGetProperty(
                   propertyName,
                   out var property) ||
               property.ValueKind !=
               JsonValueKind.False;
    }

    private static void NormalizeTableColumns(
        ReviewDraft draft)
    {
        draft.TableColumns ??= [];

        var seenIds = new HashSet<Guid>();
        var seenSpecialKinds =
            new HashSet<ReviewTableColumnKind>();

        for (var index =
                 draft.TableColumns.Count - 1;
             index >= 0;
             index--)
        {
            var column = draft.TableColumns[index];

            if (column is null ||
                !Enum.IsDefined(column.Kind) ||
                column.Kind !=
                ReviewTableColumnKind.CustomText &&
                !seenSpecialKinds.Add(column.Kind))
            {
                draft.TableColumns.RemoveAt(index);
                continue;
            }

            if (column.Id == Guid.Empty ||
                !seenIds.Add(column.Id))
            {
                column.Id = Guid.NewGuid();
                seenIds.Add(column.Id);
            }

            if (string.IsNullOrWhiteSpace(column.Heading))
            {
                column.Heading =
                    GetDefaultHeading(column.Kind);
            }
        }

        if (draft.TableColumns.Count == 0)
        {
            draft.TableColumns.Add(
                CreateColumn(
                    "Category",
                    ReviewTableColumnKind.Category));
        }
    }

    private static ReviewTableColumn CreateColumn(
        string heading,
        ReviewTableColumnKind kind)
    {
        return new ReviewTableColumn
        {
            Heading = heading,
            Kind = kind
        };
    }

    private static string GetDefaultHeading(
        ReviewTableColumnKind kind)
    {
        return kind switch
        {
            ReviewTableColumnKind.Category =>
                "Category",
            ReviewTableColumnKind.Rating =>
                "Rating",
            ReviewTableColumnKind.Note =>
                "Notes",
            _ => "New Column"
        };
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

}
