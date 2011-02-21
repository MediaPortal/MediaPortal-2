using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace HttpServer.FormDecoders
{
  /// <summary>
  /// 
  /// </summary>
  /// <remarks>
  /// http://www.faqs.org/rfcs/rfc1867.html
  /// </remarks>
  public class MultipartDecoder : IFormDecoder
  {
    /// <summary>
    /// multipart/form-data
    /// </summary>
    public const string MimeType = "multipart/form-data";

    /// <summary>
    /// form-data
    /// </summary>
    public const string FormData = "form-data";

    #region IFormDecoder Members

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stream">Stream containing the content</param>
    /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case</param>
    /// <param name="encoding">Stream enconding</param>
    /// <returns>A http form, or null if content could not be parsed.</returns>
    /// <exception cref="InvalidDataException">If contents in the stream is not valid input data.</exception>
    /// <exception cref="ArgumentNullException">If any parameter is null</exception>
    public HttpForm Decode(Stream stream, string contentType, Encoding encoding)
    {
      if (stream == null)
        throw new ArgumentNullException("stream");
      if (string.IsNullOrEmpty(contentType))
        throw new ArgumentNullException("contentType");
      if (encoding == null)
        throw new ArgumentNullException("encoding");

      if (!CanParse(contentType))
        throw new InvalidOperationException("Cannot parse contentType: " + contentType);

      //multipart/form-data, boundary=AaB03x
      int pos = contentType.IndexOf("=");
      if (pos == -1)
        throw new InvalidDataException("Missing boundry in content type.");

      string boundry = contentType.Substring(pos + 1).Trim();
      HttpMultipart multipart = new HttpMultipart(stream, boundry, encoding);

      HttpForm form = new HttpForm();

      HttpMultipart.Element element;
      while ((element = multipart.ReadNextElement()) != null)
      {
        if (string.IsNullOrEmpty(element.Name))
          throw new InvalidDataException("Error parsing request. Missing value name.\nElement: " + element);

        if (!string.IsNullOrEmpty(element.Filename))
        {
          if (string.IsNullOrEmpty(element.ContentType))
            throw new InvalidDataException("Error parsing request. Value '" + element.Name + "' lacks a content type.");

          // Read the file data
          byte[] buffer = new byte[element.Length];
          stream.Seek(element.Start, SeekOrigin.Begin);
          stream.Read(buffer, 0, (int) element.Length);

          // Generate a filename
          string filename = element.Filename;
          string internetCache = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
          // if the internet path doesn't exist, assume mono and /var/tmp
          string path = string.IsNullOrEmpty(internetCache)
              ? Path.Combine("var", "tmp")
              : Path.Combine(internetCache.Replace("\\\\", "\\"), "tmp");
          element.Filename = Path.Combine(path, Math.Abs(element.Filename.GetHashCode()) + ".tmp");

          // If the file exists generate a new filename
          while (File.Exists(element.Filename))
            element.Filename = Path.Combine(path, Math.Abs(element.Filename.GetHashCode() + 1) + ".tmp");

          if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

          File.WriteAllBytes(element.Filename, buffer);
          form.AddFile(new HttpFile(element.Name, element.Filename, element.ContentType, filename));
        }
        else
        {
          byte[] buffer = new byte[element.Length];
          stream.Seek(element.Start, SeekOrigin.Begin);
          stream.Read(buffer, 0, (int) element.Length);
          form.Add(element.Name, encoding.GetString(buffer));
        }
      }

      return form;
    }

    /// <summary>
    /// Checks if the decoder can handle the mime type
    /// </summary>
    /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case.</param>
    /// <returns>True if the decoder can parse the specified content type</returns>
    public bool CanParse(string contentType)
    {
      return contentType.StartsWith(MimeType, true, CultureInfo.InvariantCulture);
    }

    #endregion
  }
}