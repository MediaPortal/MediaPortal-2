#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Imaging;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Utilities.Graphics;

namespace MediaPortal.Common.Windows.Imaging
{
  /// <summary>
  /// <see cref="IImageTransformer"/> on GDI+. Windows only, and that is not an implementation
  /// shortcut: <c>System.Drawing.Common</c> refuses to run off Windows on .NET 6 and later, so
  /// another technology has to serve the other platforms.
  /// </summary>
  /// <remarks>
  /// The arithmetic (aspect fit, never upscale, integer scaling, default encoder quality) lives in
  /// <see cref="ImageUtilities.ResizeImage(Stream, ImageFormat, int, int)"/> rather than being
  /// restated here, on purpose. The point of extracting the interface was to move the dependency,
  /// not to change which bytes the database ends up storing.
  /// </remarks>
  public class GdiPlusImageTransformer : IImageTransformer
  {
    /// <summary>
    /// Registers this transformer as the process-wide <see cref="IImageTransformer"/>.
    /// </summary>
    /// <remarks>
    /// Called by the Windows hosts during startup, next to the other platform services they install.
    /// <c>MediaPortal.Common</c> cannot do it itself without referencing a platform assembly, which
    /// would put the dependency right back where it was.
    /// </remarks>
    public static void Register()
    {
      ServiceRegistration.Set<IImageTransformer>(new GdiPlusImageTransformer());
    }

    public bool TryEncode(Stream source, ImageType targetFormat, out byte[] imageData)
    {
      imageData = null;
      ImageFormat format = ToImageFormat(targetFormat);
      if (source == null || format == null)
        return false;
      try
      {
        // The two flags are what the fan art handler passed to Image.FromStream before this call
        // went behind the interface: validate the data and honour embedded colour management.
        using (Image image = Image.FromStream(source, true, true))
        using (Stream encoded = new MemoryStream())
        {
          image.Save(encoded, format);
          imageData = ToArray(encoded);
          return true;
        }
      }
      catch (Exception ex)
      {
        Warn("encoding", ex);
        return false;
      }
    }

    public bool TryResize(Stream source, int maxWidth, int maxHeight, ImageType targetFormat, out byte[] imageData)
    {
      imageData = null;
      ImageFormat format = ToImageFormat(targetFormat);
      if (source == null || format == null)
        return false;
      try
      {
        // Returns null for an empty stream, which is how the caller used to detect "nothing to do".
        using (Stream resized = ImageUtilities.ResizeImage(source, format, maxWidth, maxHeight))
        {
          if (resized == null)
            return false;
          imageData = ToArray(resized);
          return true;
        }
      }
      catch (Exception ex)
      {
        Warn("resizing", ex);
        return false;
      }
    }

    private static ImageFormat ToImageFormat(ImageType imageType)
    {
      switch (imageType)
      {
        case ImageType.Jpeg: return ImageFormat.Jpeg;
        case ImageType.Png: return ImageFormat.Png;
        default: return null;
      }
    }

    private static byte[] ToArray(Stream stream)
    {
      // ImageUtilities hands back a MemoryStream positioned at the start; anything else has to be
      // copied rather than read as a buffer.
      MemoryStream asMemoryStream = stream as MemoryStream;
      if (asMemoryStream != null)
        return asMemoryStream.ToArray();
      using (MemoryStream copy = new MemoryStream())
      {
        stream.Position = 0;
        stream.CopyTo(copy);
        return copy.ToArray();
      }
    }

    private static void Warn(string operation, Exception exception)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Warn("GdiPlusImageTransformer: Error {0} image", exception, operation);
      }
      catch (Exception logException)
      {
        // Logging can legitimately fail while the process shuts down; it must not replace the
        // failure the caller is about to handle.
        System.Diagnostics.Debug.WriteLine(logException);
      }
    }
  }
}
