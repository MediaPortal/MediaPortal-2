/*
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

using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace MediaPortal.Frontend.Theming
{
  /// <summary>
  /// A user's picture, shrunk the way the reference shrank it before it ever saw the server: the reference's
  /// user editor resized the picked file to at most 128x128 as a PNG (<c>UserConfigModel.cs:73-74, 406-410</c>)
  /// and stored those bytes. A picture is shown in a coin and in a row, never bigger, and a whole camera
  /// photo sent over the LAN on every user list read would be a wall the household pays for daily.
  /// </summary>
  public static class UserPicture
  {
    /// <summary>The longest edge, in pixels, exactly as the reference capped it.</summary>
    public const int MAX_EDGE = 128;

    /// <summary>
    /// Reads <paramref name="path"/>, shrinks it to <see cref="MAX_EDGE"/> at its longest edge, and answers
    /// with the PNG as base64 - the form the server's user endpoints carry. The reason a file refused is
    /// given back in words, because the press that picked it should say what went wrong rather than shrug.
    /// </summary>
    public static string? LoadShrunk(string path, out string? error)
    {
      error = null;
      try
      {
        using Bitmap source = new Bitmap(path);
        int edge = Math.Max(source.PixelSize.Width, source.PixelSize.Height);
        if (edge == 0)
        {
          error = "That picture had no size to it.";
          return null;
        }

        byte[] pngBytes;
        if (edge > MAX_EDGE)
        {
          double scale = (double)MAX_EDGE / edge;
          PixelSize target = new PixelSize(
              Math.Max(1, (int)Math.Round(source.PixelSize.Width * scale)),
              Math.Max(1, (int)Math.Round(source.PixelSize.Height * scale)));
          using Bitmap shrunk = source.CreateScaledBitmap(target);
          using MemoryStream png = new MemoryStream();
          shrunk.Save(png);
          pngBytes = png.ToArray();
        }
        else
        {
          using MemoryStream png = new MemoryStream();
          source.Save(png);
          pngBytes = png.ToArray();
        }

        return Convert.ToBase64String(pngBytes);
      }
      catch (Exception e)
      {
        error = "That file could not be read as a picture: " + e.Message;
        return null;
      }
    }

    /// <summary>
    /// Turns the base64 the server carries back into something the screen can show, or null when there is
    /// nothing - the round container then wears the name's initial like it did before pictures existed.
    /// Never throws: a half-written base64 from a store nobody trusteds is a missing picture, not a crash.
    /// </summary>
    public static Bitmap? FromBase64(string? base64)
    {
      if (string.IsNullOrEmpty(base64))
        return null;
      try
      {
        using MemoryStream png = new MemoryStream(Convert.FromBase64String(base64));
        return new Bitmap(png);
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}
