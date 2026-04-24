using Microsoft.Extensions.Localization;

namespace TaskManagement.Application.UnitTests.Common;

// Minimal IStringLocalizer stub for validator unit tests — returns the key as
// the value so tests can assert on property names (as they already do) without
// caring about actual resource lookups. Real localisation is covered by the
// API integration tests.
internal sealed class FakeStringLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(System.Globalization.CultureInfo.InvariantCulture, name, arguments), resourceNotFound: false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
