using System.Globalization;
using ErrorOr;
using Microsoft.Extensions.Localization;
using TaskManagement.Application.Resources;

namespace TaskManagement.Api.Localization;

public sealed class ErrorLocalizer : IErrorLocalizer
{
    // Metadata key convention: domain error factories that carry dynamic args
    // (ids, limits, names) put them under "args" as object[]. The localised
    // resx template uses {0}, {1}, ... placeholders which are filled here.
    // Keeping the convention in one place means domain code stays culture-free
    // and .resx files own all user-facing copy.
    internal const string ArgsMetadataKey = "args";

    private readonly IStringLocalizer<SharedResource> _localizer;

    public ErrorLocalizer(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    public string Localize(Error err)
    {
        var localized = _localizer[err.Code];
        if (localized.ResourceNotFound)
        {
            // No resx entry — fall back to the domain-supplied English description
            // so dynamic-arg info (ids, limits) survives at worst in English rather
            // than leaking the raw key or a stale static string.
            return err.Description;
        }

        if (err.Metadata is not null &&
            err.Metadata.TryGetValue(ArgsMetadataKey, out var raw) &&
            raw is object[] args &&
            args.Length > 0)
        {
            return string.Format(CultureInfo.CurrentCulture, localized.Value, args);
        }

        return localized.Value;
    }
}
