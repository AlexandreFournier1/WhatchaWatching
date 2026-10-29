using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace WhatchaWatching.Services.Email;

/// <summary>A token of Identity has characters that do not belong in an address: it travels as base 64 for urls.</summary>
public static class TokenCodec
{
    public static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static bool TryDecode(string? code, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            return token.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
