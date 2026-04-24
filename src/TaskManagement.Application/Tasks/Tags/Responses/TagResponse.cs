namespace TaskManagement.Application.Tasks.Tags.Responses;

/// <summary>Projected view of a <c>Tag</c> plus the number of tasks currently referencing it.</summary>
/// <param name="Id">Stable tag identifier.</param>
/// <param name="Name">Tag label; unique per owner.</param>
/// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
/// <param name="TaskCount">Number of tasks this owner currently has tagged with this tag.</param>
public sealed record TagResponse(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc,
    int TaskCount);
