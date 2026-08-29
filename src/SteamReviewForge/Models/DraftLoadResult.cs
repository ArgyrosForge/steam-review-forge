namespace SteamReviewForge.Models;

public enum DraftLoadStatus
{
    Empty,
    Loaded,
    Migrated,
    Invalid,
    Unsupported,
    Unavailable
}

public sealed record DraftLoadResult(
    DraftLoadStatus Status,
    ReviewDraft? Draft = null,
    string? RawBackup = null,
    bool StorageAvailable = true,
    string? Message = null);
