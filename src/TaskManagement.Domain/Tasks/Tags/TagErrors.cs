using ErrorOr;

namespace TaskManagement.Domain.Tasks.Tags;

public static class TagErrors
{
    // Dynamic args flow through Metadata["args"] so the API-edge ErrorLocalizer
    // can substitute them into the localised resx template. The English
    // Description is kept for logs and for the ResourceNotFound fallback path.
    public static Error NotFound(TagId id) =>
        Error.NotFound(
            "Tag.NotFound",
            $"Tag with id '{id}' was not found.",
            metadata: Args(id.ToString()));

    public static Error NotFoundMany(IEnumerable<TagId> ids) =>
        Error.NotFound(
            "Tag.NotFound",
            $"Tags with ids {string.Join(", ", ids.Select(i => $"'{i}'"))} were not found.",
            metadata: Args(string.Join(", ", ids.Select(i => $"'{i}'"))));

    public static readonly Error OwnerRequired =
        Error.Validation("Tag.OwnerRequired", "Tag owner is required.");

    public static readonly Error NameRequired =
        Error.Validation("Tag.NameRequired", "Tag name must not be empty.");

    public static Error NameTooLong(int maxLength) =>
        Error.Validation(
            "Tag.NameTooLong",
            $"Tag name must not exceed {maxLength} characters.",
            metadata: Args(maxLength));

    public static Error AlreadyExists(string name) =>
        Error.Conflict(
            "Tag.AlreadyExists",
            $"A tag named '{name}' already exists for this user.",
            metadata: Args(name));

    private static Dictionary<string, object> Args(params object[] values) =>
        new() { ["args"] = values };
}
