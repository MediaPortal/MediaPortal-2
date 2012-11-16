#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;
using System.IO.Compression;
using System.Net;
using HttpServer;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// <see cref="CompressionHelper"/> provides methods to compress and decrompress Streams using gzip compression.
  /// </summary>
  static class CompressionHelper
  {
    public const string PREFERRED_COMPRESSION = "gzip";
    
    private const int BUFFER_SIZE = 1024;

    // For debugging purpose
    private const bool DISABLE_COMPRESSION = false;

    /// <summary>
    /// Compresses the content of the given <paramref name="inputStream"/> and returns a compressed Stream.
    /// </summary>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Compressed stream</returns>
    private static Stream Compress(Stream inputStream)
    {
      MemoryStream compressed = new MemoryStream();
      using (var zip = new GZipStream(compressed, CompressionMode.Compress, true))
      {
        byte[] buffer = new byte[BUFFER_SIZE];
        int nRead;
        while ((nRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
          zip.Write(buffer, 0, nRead);
      }
      compressed.Position = 0;
      return compressed;
    }

    /// <summary>
    /// Decompresses the content of the given <paramref name="inputStream"/> and returns an uncompressed Stream.
    /// </summary>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Unompressed stream</returns>
    public static Stream Decompress(Stream inputStream)
    {
      MemoryStream decompressed = new MemoryStream();
      using (Stream csStream = new GZipStream(inputStream, CompressionMode.Decompress))
      {
        byte[] buffer = new byte[BUFFER_SIZE];
        int nRead;
        while ((nRead = csStream.Read(buffer, 0, buffer.Length)) > 0)
          decompressed.Write(buffer, 0, nRead);
      }
      decompressed.Position = 0;
      return decompressed;
    }

    /// <summary>
    /// Helper method to handle decompression of <see cref="WebResponse"/> streams. If the header contains a "Content-Encoding" for type "gzip", the
    /// returned stream will automatically be decompressed.
    /// </summary>
    /// <param name="response">Response to process</param>
    /// <returns>Decompressed stream.</returns>
    public static Stream Decompress(WebResponse response)
    {
      bool isGzip = response.Headers.Get("CONTENT-ENCODING") == PREFERRED_COMPRESSION;
      return isGzip ? Decompress(response.GetResponseStream()) : response.GetResponseStream();
    }

    /// <summary>
    /// Helper method to write the contents of <paramref name="inputStream"/> to the <paramref name="response"/> body. Depending on the
    /// <paramref name="compress"/> argument, the result will be compressed or not.
    /// </summary>
    /// <param name="response">Response to be written</param>
    /// <param name="inputStream">Input stream</param>
    /// <param name="compress"><c>true</c> to compress the result</param>
    public static void WriteCompressedStream(IHttpResponse response, MemoryStream inputStream, bool compress)
    {
      byte[] buffer;
      if (!compress || DISABLE_COMPRESSION)
      {
        buffer = inputStream.ToArray();
        response.ContentLength = buffer.Length;
        response.Body.Write(buffer, 0, buffer.Length);
        return;
      }

      using (MemoryStream compressedStream = (MemoryStream) Compress(inputStream))
        buffer = compressedStream.ToArray();

      response.AddHeader("Content-Encoding", PREFERRED_COMPRESSION);
      response.ContentLength = buffer.Length;
      response.Body.Write(buffer, 0, buffer.Length);
    }
  }
}
