namespace TaskManagement.Domain.Tasks.Tags;

public readonly record struct TagId(Guid Value)
{
    public static TagId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
