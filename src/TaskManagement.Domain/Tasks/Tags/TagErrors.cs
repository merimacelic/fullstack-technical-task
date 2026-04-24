using ErrorOr;

namespace TaskManagement.Domain.Tasks.Tags;

public static class TagErrors
{
    public static Error NotFound(TagId id) =>
        Error.NotFound("Tag.NotFound", $"Tag with id '{id}' was not found.");

    public static Error NotFoundMany(IEnumerable<TagId> ids) =>
        Error.NotFound(
            "Tag.NotFound",
            $"Tags with ids {string.Join(", ", ids.Select(i => $"'{i}'"))} were not found.");

    public static readonly Error OwnerRequired =
        Error.Validation("Tag.OwnerRequired", "Tag owner is required.");

    public static readonly Error NameRequired =
        Error.Validation("Tag.NameRequired", "Tag name must not be empty.");

    public static Error NameTooLong(int maxLength) =>
        Error.Validation("Tag.NameTooLong", $"Tag name must not exceed {maxLength} characters.");

    public static Error AlreadyExists(string name) =>
        Error.Conflict("Tag.AlreadyExists", $"A tag named '{name}' already exists for this user.");
}
