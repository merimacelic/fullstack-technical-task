using ErrorOr;

namespace TaskManagement.Domain.Users;

public static class UserErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error EmailRequired =
        Error.Validation("User.EmailRequired", "Email is required.");

    public static readonly Error PasswordRequired =
        Error.Validation("User.PasswordRequired", "Password is required.");

    // Deliberately generic: a precise reason (e.g. "email already in use" vs
    // "password too weak") would let a caller enumerate registered emails.
    public static readonly Error RegistrationFailed =
        Error.Validation(
            "User.RegistrationFailed",
            "Registration failed. Check the email and password, then try again.");

    public static readonly Error LockedOut =
        Error.Unauthorized("User.LockedOut", "Account is locked due to repeated failed attempts.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("User.InvalidRefreshToken", "Refresh token is invalid, expired, or revoked.");
}
