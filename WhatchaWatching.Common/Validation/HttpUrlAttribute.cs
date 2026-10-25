using System.ComponentModel.DataAnnotations;

namespace WhatchaWatching.Common.Validation;

/// <summary>An empty value is valid; otherwise it must be an absolute http or https address (never javascript: and the like).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute() : base("Enter a full web address starting with http:// or https://.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null or "" ||
        (value is string text &&
         Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri) &&
         uri.Scheme is "http" or "https");
}
