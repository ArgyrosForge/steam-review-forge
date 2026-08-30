using System.Text.Json;
using Microsoft.JSInterop;
using SteamReviewForge.Models;

namespace SteamReviewForge.Services;

public sealed class ReviewDraftStorageService
{
    private const string CurrentStorageKey = "steam-review-forge-draft-v2";
    private const string LegacyStorageKey = "steam-review-forge-draft-v1";
    private const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _js;

    public ReviewDraftStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async ValueTask<DraftLoadResult> LoadAsync()
    {
        string? currentJson;

        try
        {
            currentJson = await GetAsync(CurrentStorageKey);
        }
        catch (Exception exception)
        {
            return Unavailable(exception);
        }

        if (!string.IsNullOrWhiteSpace(currentJson))
        {
            var currentResult = LoadCurrent(currentJson);

            if (currentResult.Status != DraftLoadStatus.Migrated ||
                currentResult.Draft is null)
            {
                return currentResult;
            }

            try
            {
                await SaveAsync(currentResult.Draft);
                return currentResult;
            }
            catch (Exception exception)
            {
                return currentResult with
                {
                    StorageAvailable = false,
                    Message =
                        "Draft restored, but the upgraded copy could not be saved: " +
                        exception.Message
                };
            }
        }

        string? legacyJson;

        try
        {
            legacyJson = await GetAsync(LegacyStorageKey);
        }
        catch (Exception exception)
        {
            return Unavailable(exception);
        }

        if (string.IsNullOrWhiteSpace(legacyJson))
        {
            return new DraftLoadResult(DraftLoadStatus.Empty);
        }

        var legacyResult = LoadLegacy(legacyJson);

        if (legacyResult.Draft is null)
        {
            return legacyResult;
        }

        try
        {
            await SaveAsync(legacyResult.Draft);
            await RemoveAsync(LegacyStorageKey);

            return legacyResult with
            {
                Status = DraftLoadStatus.Migrated,
                Message = "Draft upgraded to the current format."
            };
        }
        catch (Exception exception)
        {
            return legacyResult with
            {
                Status = DraftLoadStatus.Migrated,
                StorageAvailable = false,
                Message =
                    "Draft restored, but the upgraded copy could not be saved: " +
                    exception.Message
            };
        }
    }

    public ValueTask SaveAsync(ReviewDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var envelope = new PersistedDraftEnvelope
        {
            SchemaVersion = CurrentSchemaVersion,
            Draft = draft
        };

        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        return _js.InvokeVoidAsync(
            "reviewDraftStorage.set",
            CurrentStorageKey,
            json);
    }

    public async ValueTask ClearAsync()
    {
        await RemoveAsync(CurrentStorageKey);
        await RemoveAsync(LegacyStorageKey);
    }

    private DraftLoadResult LoadCurrent(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("schemaVersion", out var versionProperty) ||
                versionProperty.ValueKind != JsonValueKind.Number ||
                !versionProperty.TryGetInt32(out var schemaVersion) ||
                schemaVersion < 1)
            {
                return Invalid(json, "The saved draft has no valid schema version.");
            }

            if (schemaVersion > CurrentSchemaVersion)
            {
                return new DraftLoadResult(
                    DraftLoadStatus.Unsupported,
                    RawBackup: json,
                    Message:
                        $"This draft uses newer schema version {schemaVersion}. " +
                        $"This build supports up to version {CurrentSchemaVersion}.");
            }

            if (!root.TryGetProperty("draft", out var draftProperty) ||
                draftProperty.ValueKind != JsonValueKind.Object)
            {
                return Invalid(json, "The saved draft does not contain review data.");
            }

            var draft = draftProperty.Deserialize<ReviewDraft>(JsonOptions);

            if (draft is null)
            {
                return Invalid(json, "The saved review data could not be read.");
            }

            Normalize(draft);

            return new DraftLoadResult(
                schemaVersion < CurrentSchemaVersion
                    ? DraftLoadStatus.Migrated
                    : DraftLoadStatus.Loaded,
                draft,
                Message: schemaVersion < CurrentSchemaVersion
                    ? "Draft upgraded to the current format."
                    : null);
        }
        catch (JsonException exception)
        {
            return Invalid(
                json,
                $"The saved draft contains invalid JSON: {exception.Message}");
        }
        catch (NotSupportedException exception)
        {
            return Invalid(
                json,
                $"The saved draft format is unsupported: {exception.Message}");
        }
    }

    private DraftLoadResult LoadLegacy(string json)
    {
        try
        {
            var draft = JsonSerializer.Deserialize<ReviewDraft>(json, JsonOptions);

            if (draft is null)
            {
                return Invalid(json, "The legacy draft could not be read.");
            }

            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("tableColumns", out _))
            {
                draft.TableColumns = CreateLegacyTableColumns(document.RootElement);
            }

            Normalize(draft);

            return new DraftLoadResult(DraftLoadStatus.Migrated, draft);
        }
        catch (JsonException exception)
        {
            return Invalid(
                json,
                $"The legacy draft contains invalid JSON: {exception.Message}");
        }
        catch (NotSupportedException exception)
        {
            return Invalid(
                json,
                $"The legacy draft format is unsupported: {exception.Message}");
        }
    }

    private static void Normalize(ReviewDraft draft)
    {
        if (!Enum.IsDefined(draft.EditingMode))
        {
            draft.EditingMode = ReviewEditingMode.GuidedStructured;
        }

        if (!Enum.IsDefined(draft.DisplayFormat))
        {
            draft.DisplayFormat = ReviewDisplayFormat.RatingTable;
        }

        if (!Enum.IsDefined(draft.RatingSystem))
        {
            draft.RatingSystem = ReviewRatingSystem.FiveStars;
        }

        if (!Enum.IsDefined(draft.Template))
        {
            draft.Template = ReviewTemplate.Balanced;
        }

        if (draft.Recommendation is not null &&
            !Enum.IsDefined(draft.Recommendation.Value))
        {
            draft.Recommendation = null;
        }

        draft.RawBbCode ??= string.Empty;
        draft.Title ??= string.Empty;
        draft.Summary ??= string.Empty;
        draft.Playtime = PlaytimeFormatter.Normalize(draft.Playtime);
        draft.WhatWorks ??= string.Empty;
        draft.WhatWorksHeading ??= "What Works";
        draft.WhatCouldBeBetter ??= string.Empty;
        draft.WhatCouldBeBetterHeading ??= "What Could Be Better";
        draft.FinalThoughts ??= string.Empty;
        draft.FinalThoughtsHeading ??= "Final Thoughts";

        draft.TextRatingOptions ??= [];

        for (var index = 0; index < draft.TextRatingOptions.Count; index++)
        {
            draft.TextRatingOptions[index] ??= string.Empty;
        }

        if (draft.TextRatingOptions.Count == 0)
        {
            draft.TextRatingOptions.Add("Rating");
        }

        NormalizeTableColumns(draft);
        NormalizeCategories(draft);
        NormalizeComponents(draft);
    }

    private static void NormalizeCategories(ReviewDraft draft)
    {
        draft.Categories ??= [];

        var seenIds = new HashSet<Guid>();
        var maximumRating = GetMaximumRating(draft);

        for (var index = draft.Categories.Count - 1; index >= 0; index--)
        {
            var category = draft.Categories[index];

            if (category is null)
            {
                draft.Categories.RemoveAt(index);
                continue;
            }

            if (category.Id == Guid.Empty || !seenIds.Add(category.Id))
            {
                category.Id = Guid.NewGuid();
                seenIds.Add(category.Id);
            }

            category.Name ??= string.Empty;
            category.Note ??= string.Empty;
            category.CustomCells ??= [];

            foreach (var key in category.CustomCells.Keys.ToArray())
            {
                category.CustomCells[key] ??= string.Empty;
            }

            category.Rating = Math.Clamp(category.Rating, 1, maximumRating);
        }

        if (draft.Categories.Count == 0 &&
            draft.DisplayFormat != ReviewDisplayFormat.MinimalVerdict)
        {
            draft.Categories.Add(new ReviewCategory());
        }
    }

    private static void NormalizeComponents(ReviewDraft draft)
    {
        draft.Components ??= [];

        var seenIds = new HashSet<Guid>();
        var maximumRating = GetMaximumRating(draft);

        for (var index = draft.Components.Count - 1; index >= 0; index--)
        {
            var component = draft.Components[index];

            if (component is null || !Enum.IsDefined(component.Kind))
            {
                draft.Components.RemoveAt(index);
                continue;
            }

            if (component.Id == Guid.Empty || !seenIds.Add(component.Id))
            {
                component.Id = Guid.NewGuid();
                seenIds.Add(component.Id);
            }

            component.Heading ??= "New Section";
            component.Content ??= string.Empty;
            component.Rating = Math.Clamp(component.Rating, 1, maximumRating);
        }
    }

    private static int GetMaximumRating(ReviewDraft draft)
    {
        return draft.RatingSystem switch
        {
            ReviewRatingSystem.TenPoint => 10,
            ReviewRatingSystem.Text => Math.Max(1, draft.TextRatingOptions.Count),
            _ => 5
        };
    }

    private static List<ReviewTableColumn> CreateLegacyTableColumns(JsonElement root)
    {
        var columns = new List<ReviewTableColumn>();

        if (GetLegacyColumnVisibility(root, "showCategoryColumn"))
        {
            columns.Add(CreateColumn("Category", ReviewTableColumnKind.Category));
        }

        if (GetLegacyColumnVisibility(root, "showRatingColumn"))
        {
            columns.Add(CreateColumn("Rating", ReviewTableColumnKind.Rating));
        }

        if (GetLegacyColumnVisibility(root, "showNoteColumn"))
        {
            columns.Add(CreateColumn("Notes", ReviewTableColumnKind.Note));
        }

        if (columns.Count == 0)
        {
            columns.Add(CreateColumn("Category", ReviewTableColumnKind.Category));
        }

        return columns;
    }

    private static bool GetLegacyColumnVisibility(JsonElement root, string propertyName)
    {
        return !root.TryGetProperty(propertyName, out var property) ||
               property.ValueKind != JsonValueKind.False;
    }

    private static void NormalizeTableColumns(ReviewDraft draft)
    {
        draft.TableColumns ??= [];

        var seenIds = new HashSet<Guid>();
        var seenSpecialKinds = new HashSet<ReviewTableColumnKind>();

        for (var index = draft.TableColumns.Count - 1; index >= 0; index--)
        {
            var column = draft.TableColumns[index];

            if (column is null ||
                !Enum.IsDefined(column.Kind) ||
                column.Kind != ReviewTableColumnKind.CustomText &&
                !seenSpecialKinds.Add(column.Kind))
            {
                draft.TableColumns.RemoveAt(index);
                continue;
            }

            if (column.Id == Guid.Empty || !seenIds.Add(column.Id))
            {
                column.Id = Guid.NewGuid();
                seenIds.Add(column.Id);
            }

            column.Heading = string.IsNullOrWhiteSpace(column.Heading)
                ? GetDefaultHeading(column.Kind)
                : column.Heading;
        }

        if (draft.TableColumns.Count == 0)
        {
            draft.TableColumns.Add(CreateColumn("Category", ReviewTableColumnKind.Category));
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

    private static string GetDefaultHeading(ReviewTableColumnKind kind)
    {
        return kind switch
        {
            ReviewTableColumnKind.Category => "Category",
            ReviewTableColumnKind.Rating => "Rating",
            ReviewTableColumnKind.Note => "Notes",
            _ => "New Column"
        };
    }

    private ValueTask<string?> GetAsync(string key)
    {
        return _js.InvokeAsync<string?>("reviewDraftStorage.get", key);
    }

    private ValueTask RemoveAsync(string key)
    {
        return _js.InvokeVoidAsync("reviewDraftStorage.remove", key);
    }

    private static DraftLoadResult Invalid(string rawBackup, string message)
    {
        return new DraftLoadResult(
            DraftLoadStatus.Invalid,
            RawBackup: rawBackup,
            Message: message);
    }

    private static DraftLoadResult Unavailable(Exception exception)
    {
        return new DraftLoadResult(
            DraftLoadStatus.Unavailable,
            StorageAvailable: false,
            Message: exception.Message);
    }

    private sealed class PersistedDraftEnvelope
    {
        public int SchemaVersion { get; set; }

        public ReviewDraft? Draft { get; set; }
    }
}
