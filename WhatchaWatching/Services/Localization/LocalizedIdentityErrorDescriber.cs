using Microsoft.AspNetCore.Identity;

namespace WhatchaWatching.Services.Localization;

/// <summary>
/// The messages Identity gives for a refused password, a taken username and so on, in the language of the visit. The
/// English texts are those of Identity itself, which are also the keys of the translations.
/// </summary>
public sealed class LocalizedIdentityErrorDescriber(ILocalizer localizer) : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => With(base.DefaultError(), "An unknown failure has occurred.");

    public override IdentityError ConcurrencyFailure() =>
        With(base.ConcurrencyFailure(), "Optimistic concurrency failure, object has been modified.");

    public override IdentityError PasswordMismatch() => With(base.PasswordMismatch(), "Incorrect password.");

    public override IdentityError InvalidToken() => With(base.InvalidToken(), "Invalid token.");

    public override IdentityError InvalidUserName(string? userName) =>
        With(base.InvalidUserName(userName), "Username '{0}' is invalid, can only contain letters or digits.", userName);

    public override IdentityError InvalidEmail(string? email) =>
        With(base.InvalidEmail(email), "Email '{0}' is invalid.", email);

    public override IdentityError DuplicateUserName(string userName) =>
        With(base.DuplicateUserName(userName), "Username '{0}' is already taken.", userName);

    public override IdentityError DuplicateEmail(string email) =>
        With(base.DuplicateEmail(email), "Email '{0}' is already taken.", email);

    public override IdentityError PasswordTooShort(int length) =>
        With(base.PasswordTooShort(length), "Passwords must be at least {0} characters.", length);

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        With(base.PasswordRequiresNonAlphanumeric(), "Passwords must have at least one non alphanumeric character.");

    public override IdentityError PasswordRequiresDigit() =>
        With(base.PasswordRequiresDigit(), "Passwords must have at least one digit ('0'-'9').");

    public override IdentityError PasswordRequiresLower() =>
        With(base.PasswordRequiresLower(), "Passwords must have at least one lowercase ('a'-'z').");

    public override IdentityError PasswordRequiresUpper() =>
        With(base.PasswordRequiresUpper(), "Passwords must have at least one uppercase ('A'-'Z').");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        With(base.PasswordRequiresUniqueChars(uniqueChars), "Passwords must use at least {0} different characters.", uniqueChars);

    // The code stays the one of Identity: the pages and the tests compare codes, not texts.
    private IdentityError With(IdentityError error, string english, params object?[] args) =>
        new() { Code = error.Code, Description = args.Length == 0 ? localizer[english] : localizer.F(english, args) };
}
