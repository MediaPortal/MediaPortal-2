using System;
using System.Web;

namespace MediaPortal.Common.Network
{
  /// <summary>
  /// <see cref="UriExtension"/> provides a custom escaping methods for special characters like ampersands. Without special handling, properly uri encoded
  /// ampersands are wrongly decoded inside <see cref="Uri.ToString"/> and also <see cref="HttpRequest.Params"/>.
  /// </summary>
  public static class UriExtension
  {
    /// <summary>
    /// Encodes the given <paramref name="value"/> into a valid uri string.
    /// </summary>
    /// <param name="value">Decoded string.</param>
    /// <returns>Encoded string.</returns>
    public static string Encode(this string value)
    {
      return HttpUtility.UrlEncode(Escape(value));
    }

    /// <summary>
    /// Decodes the given <paramref name="value"/> into original text.
    /// </summary>
    /// <param name="value">Encoded string.</param>
    /// <returns>Decoded string.</returns>
    public static string Decode(this string value)
    {
      return Unescape(value);
    }

    private static string Escape(string value)
    {
      return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("&", "|*|");
    }

    private static string Unescape(string value)
    {
      return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("|*|", "&");
    }
  }
}
