using ErrorOr;

namespace TaskManagement.Api.Localization;

// Edge-of-API translator for domain/application errors. Given an Error whose
// Code matches a key in SharedResource (e.g. "Task.NotFound"), returns the
// localised description for the current request culture. Falls back to the
// error's own Description when no resource key exists — so dynamic-arg errors
// without a matching resource still round-trip sensibly.
public interface IErrorLocalizer
{
    string Localize(Error err);
}
