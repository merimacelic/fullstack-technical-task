using System.Reflection;

namespace TaskManagement.Domain.Common;

// Smart-enum base: each value is a static instance of the concrete type with a
// numeric Id + string Name, enabling behavior-on-values and safe persistence as
// integers or strings.
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<IReadOnlyDictionary<int, TEnum>> ById = new(() =>
        GetAll().ToDictionary(e => e.Id));

    private static readonly Lazy<IReadOnlyDictionary<string, TEnum>> ByName = new(() =>
        GetAll().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    protected Enumeration(int id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name;
    }

    public int Id { get; }

    public string Name { get; }

    public static IEnumerable<TEnum> GetAll() =>
        typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => typeof(TEnum).IsAssignableFrom(f.FieldType))
            .Select(f => f.GetValue(null))
            .Cast<TEnum>();

    public static TEnum FromId(int id) =>
        ById.Value.TryGetValue(id, out var value)
            ? value
            : throw new InvalidOperationException($"No {typeof(TEnum).Name} with Id '{id}'.");

    public static TEnum FromName(string name) =>
        ByName.Value.TryGetValue(name, out var value)
            ? value
            : throw new InvalidOperationException($"No {typeof(TEnum).Name} with Name '{name}'.");

    public static bool TryFromName(string name, out TEnum? value) =>
        ByName.Value.TryGetValue(name, out value);

    public bool Equals(Enumeration<TEnum>? other) =>
        other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj) => obj is Enumeration<TEnum> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public int CompareTo(Enumeration<TEnum>? other) =>
        other is null ? 1 : Id.CompareTo(other.Id);

    public override string ToString() => Name;

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right) => !(left == right);

    public static bool operator <(Enumeration<TEnum> left, Enumeration<TEnum> right) => left.CompareTo(right) < 0;

    public static bool operator >(Enumeration<TEnum> left, Enumeration<TEnum> right) => left.CompareTo(right) > 0;

    public static bool operator <=(Enumeration<TEnum> left, Enumeration<TEnum> right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Enumeration<TEnum> left, Enumeration<TEnum> right) => left.CompareTo(right) >= 0;
}
