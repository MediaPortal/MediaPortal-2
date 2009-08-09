using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HttpServer.FormDecoders
{
  /// <summary>
  /// Can handle application/x-www-form-urlencoded
  /// </summary>
  public class UrlDecoder : IFormDecoder
  {
    #region IFormDecoder Members

    /// <summary>
    /// </summary>
    /// <param name="stream">Stream containing the content</param>
    /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case</param>
    /// <param name="encoding">Stream encoding</param>
    /// <returns>
    /// A HTTP form, or null if content could not be parsed.
    /// </returns>
    /// <exception cref="InvalidDataException">If contents in the stream is not valid input data.</exception>
    public HttpForm Decode(Stream stream, string contentType, Encoding encoding)
    {
      if (stream == null || stream.Length == 0)
        return null;
      if (!CanParse(contentType))
        return null;
      if (encoding == null)
        encoding = Encoding.UTF8;

      try
      {
        StreamReader reader = new StreamReader(stream, encoding);
        return new HttpForm(HttpHelper.ParseQueryString(reader.ReadToEnd()));
      }
      catch (ArgumentException err)
      {
        throw new InvalidDataException(err.Message, err);
      }
    }


    /// <summary>
    /// Checks if the decoder can handle the mime type
    /// </summary>
    /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case.</param>
    /// <returns>True if the decoder can parse the specified content type</returns>
    public bool CanParse(string contentType)
    {
      return !string.IsNullOrEmpty(contentType) &&
          contentType.StartsWith("application/x-www-form-urlencoded", true, CultureInfo.InvariantCulture);
    }

    #endregion
  }
}