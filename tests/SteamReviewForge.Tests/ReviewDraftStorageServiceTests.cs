using System.Text.Json;
using SteamReviewForge.Models;
using SteamReviewForge.Services;
using Xunit;

namespace SteamReviewForge.Tests;

public sealed class ReviewDraftStorageServiceTests
{
    private const string CurrentKey = "steam-review-forge-draft-v2";
    private const string LegacyKey = "steam-review-forge-draft-v1";

    [Fact]
    public async Task LoadAsync_ReturnsEmpty_WhenNoDraftExists()
    {
        var service = new ReviewDraftStorageService(new FakeJsRuntime());

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Empty, result.Status);
        Assert.Null(result.Draft);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsCurrentEnvelopeAndStableCategoryId()
    {
        var js = new FakeJsRuntime();
        var service = new ReviewDraftStorageService(js);
        var draft = new ReviewDraft
        {
            Recommendation = ReviewRecommendation.Recommended,
            WhatWorksHeading = "Highlights",
            IncludeCategoryDivider = false,
            IsEarlyAccessReview = true,
            TableCellWidthMode = ReviewTableCellWidthMode.Automatic
        };
        var categoryId = draft.Categories[0].Id;

        await service.SaveAsync(draft);
        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Draft);
        Assert.Equal(categoryId, result.Draft.Categories[0].Id);
        Assert.Equal("Highlights", result.Draft.WhatWorksHeading);
        Assert.False(result.Draft.IncludeCategoryDivider);
        Assert.True(result.Draft.IsEarlyAccessReview);
        Assert.Equal(
            ReviewTableCellWidthMode.Automatic,
            result.Draft.TableCellWidthMode);

        using var document = JsonDocument.Parse(js.Storage[CurrentKey]);
        Assert.Equal(3, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("draft", out _));
    }

    [Fact]
    public async Task LoadAsync_MigratesLegacyDraftAndRemovesLegacyKey()
    {
        var js = new FakeJsRuntime();
        var categoryId = Guid.NewGuid();
        js.Storage[LegacyKey] = $$"""
            {
              "summary": "Legacy review",
              "recommendation": 0,
              "categories": [
                {
                  "id": "{{categoryId}}",
                  "name": "Gameplay",
                  "rating": 4,
                  "note": "Good",
                  "customCells": {}
                }
              ]
            }
            """;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Migrated, result.Status);
        Assert.True(result.StorageAvailable);
        Assert.Equal(categoryId, result.Draft!.Categories[0].Id);
        Assert.Contains(CurrentKey, js.Storage.Keys);
        Assert.DoesNotContain(LegacyKey, js.Storage.Keys);
    }

    [Fact]
    public async Task LoadAsync_PreservesMalformedPayloadForRecovery()
    {
        var js = new FakeJsRuntime();
        const string malformed = "{ definitely-not-json";
        js.Storage[CurrentKey] = malformed;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Invalid, result.Status);
        Assert.Equal(malformed, result.RawBackup);
        Assert.Null(result.Draft);
        Assert.Equal(malformed, js.Storage[CurrentKey]);
    }

    [Fact]
    public async Task LoadAsync_PreservesNewerSchemaForRecovery()
    {
        var js = new FakeJsRuntime();
        const string future = "{\"schemaVersion\":99,\"draft\":{}}";
        js.Storage[CurrentKey] = future;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Unsupported, result.Status);
        Assert.Equal(future, result.RawBackup);
        Assert.Contains("99", result.Message);
    }

    [Fact]
    public async Task LoadAsync_UpgradesOlderEnvelopeSchema()
    {
        var js = new FakeJsRuntime();
        js.Storage[CurrentKey] = """
            {
              "schemaVersion": 1,
              "draft": {
                "summary": "Older envelope",
                "recommendation": 0
              }
            }
            """;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Migrated, result.Status);
        using var upgraded = JsonDocument.Parse(js.Storage[CurrentKey]);
        Assert.Equal(
            3,
            upgraded.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public async Task LoadAsync_MigratesVersionTwoBodyFormatsWithoutChangingLegacyComponents()
    {
        var js = new FakeJsRuntime();
        js.Storage[CurrentKey] = """
            {
              "schemaVersion": 2,
              "draft": {
                "whatWorks": "One\nTwo",
                "whatCouldBeBetter": "Three\nFour",
                "finalThoughts": "Five",
                "components": [
                  {
                    "id": "c6712c87-f67b-4aaa-a402-34eb24e84e13",
                    "kind": 1,
                    "heading": "Legacy list",
                    "content": "Six\nSeven",
                    "rating": 3
                  },
                  {
                    "id": "ab6b76c5-55b0-440f-8903-cd43e81554ba",
                    "kind": 0,
                    "heading": "Legacy rating",
                    "content": "Notes",
                    "rating": 3
                  }
                ]
              }
            }
            """;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Migrated, result.Status);
        Assert.Equal(ReviewTextFormat.Text, result.Draft!.WhatWorksFormat);
        Assert.Equal(ReviewTextFormat.Text, result.Draft.WhatCouldBeBetterFormat);
        Assert.Equal(ReviewTextFormat.Text, result.Draft.FinalThoughtsFormat);
        Assert.Equal(
            ReviewTextFormat.BulletedList,
            result.Draft.Components[0].ContentFormat);
        Assert.Equal(
            ReviewTextFormat.Text,
            result.Draft.Components[1].ContentFormat);

        using var upgraded = JsonDocument.Parse(js.Storage[CurrentKey]);
        Assert.Equal(
            3,
            upgraded.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public async Task LoadAsync_NormalizesInvalidAndDuplicateValues()
    {
        var duplicateId = Guid.NewGuid();
        var js = new FakeJsRuntime();
        js.Storage[CurrentKey] = $$"""
            {
              "schemaVersion": 2,
              "draft": {
                "editingMode": 99,
                "displayFormat": 99,
                "ratingSystem": 99,
                "template": 99,
                "recommendation": 1,
                "playtime": "12.34",
                "textRatingOptions": [],
                "tableColumns": [],
                "categories": [
                  { "id": "{{duplicateId}}", "name": null, "rating": 99, "note": null, "customCells": null },
                  { "id": "{{duplicateId}}", "name": "Second", "rating": -1, "note": "", "customCells": {} },
                  null
                ],
                "components": [null]
              }
            }
            """;
        var service = new ReviewDraftStorageService(js);

        var result = await service.LoadAsync();
        var draft = result.Draft!;

        Assert.Equal(ReviewEditingMode.GuidedStructured, draft.EditingMode);
        Assert.Equal(ReviewDisplayFormat.RatingTable, draft.DisplayFormat);
        Assert.Equal(ReviewRatingSystem.FiveStars, draft.RatingSystem);
        Assert.Equal(ReviewTemplate.Balanced, draft.Template);
        Assert.Null(draft.Recommendation);
        Assert.Equal("12.3", draft.Playtime);
        Assert.Equal("Rating", Assert.Single(draft.TextRatingOptions));
        Assert.NotEmpty(draft.TableColumns);
        Assert.Equal(2, draft.Categories.Count);
        Assert.NotEqual(draft.Categories[0].Id, draft.Categories[1].Id);
        Assert.All(draft.Categories, category => Assert.InRange(category.Rating, 1, 5));
        Assert.Empty(draft.Components);
    }

    [Fact]
    public async Task LoadAsync_DistinguishesStorageFailure()
    {
        var service = new ReviewDraftStorageService(
            new FakeJsRuntime { ThrowOnAccess = true });

        var result = await service.LoadAsync();

        Assert.Equal(DraftLoadStatus.Unavailable, result.Status);
        Assert.False(result.StorageAvailable);
        Assert.Null(result.RawBackup);
    }

    [Fact]
    public async Task ClearAsync_RemovesCurrentAndLegacyPayloads()
    {
        var js = new FakeJsRuntime();
        js.Storage[CurrentKey] = "current";
        js.Storage[LegacyKey] = "legacy";
        var service = new ReviewDraftStorageService(js);

        await service.ClearAsync();

        Assert.Empty(js.Storage);
    }
}
