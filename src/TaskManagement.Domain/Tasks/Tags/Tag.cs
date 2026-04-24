using ErrorOr;
using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks.Tags;

public sealed class Tag : AggregateRoot<TagId>
{
    public const int MaxNameLength = 50;

    private Tag(TagId id, Guid ownerId, string name, DateTime createdAtUtc)
        : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    // EF Core materialization constructor.
    private Tag()
    {
        Name = string.Empty;
    }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static ErrorOr<Tag> Create(Guid ownerId, string name, DateTime nowUtc)
    {
        if (ownerId == Guid.Empty)
        {
            return TagErrors.OwnerRequired;
        }

        var normalized = NormalizeName(name);
        var validation = ValidateName(normalized);
        if (validation.IsError)
        {
            return validation.Errors;
        }

        return new Tag(TagId.New(), ownerId, normalized, nowUtc);
    }

    public ErrorOr<Success> Rename(string name)
    {
        var normalized = NormalizeName(name);
        var validation = ValidateName(normalized);
        if (validation.IsError)
        {
            return validation.Errors;
        }

        Name = normalized;
        return Result.Success;
    }

    private static ErrorOr<Success> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return TagErrors.NameRequired;
        }

        if (name.Length > MaxNameLength)
        {
            return TagErrors.NameTooLong(MaxNameLength);
        }

        return Result.Success;
    }

    private static string NormalizeName(string name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
}
