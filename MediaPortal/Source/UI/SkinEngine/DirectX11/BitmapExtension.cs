#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using SharpDX.Direct2D1;
using SharpDX.IO;
using SharpDX.WIC;

namespace MediaPortal.UI.SkinEngine.DirectX11
{
  public static class BitmapExtension
  {
    /// <summary>
    /// Saves the <paramref name="bitmap"/> into a file, specified by <paramref name="outputPath"/>. Existing files will be overwritten by this method.
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <param name="outputPath">Output path</param>
    public static void Save(this Bitmap1 bitmap, string outputPath)
    {
      // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
      // IMAGE SAVING ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

      // delete the output file if it already exists
      if (File.Exists(outputPath)) File.Delete(outputPath);

      // use the appropiate overload to write either to stream or to a file
      using (var stream = new WICStream(GraphicsDevice11.Instance.FactoryWIC, outputPath, NativeFileAccess.Write))
      using (var encoder = new PngBitmapEncoder(GraphicsDevice11.Instance.FactoryWIC))
      {
        encoder.Initialize(stream);

        var wicPixelFormat = SharpDX.WIC.PixelFormat.Format32bppPRGBA;
        int pixelWidth = (int)bitmap.Size.Width;
        int pixelHeight = (int)bitmap.Size.Height;
        using (var bitmapFrameEncode = new BitmapFrameEncode(encoder))
        {
          bitmapFrameEncode.Initialize();
          bitmapFrameEncode.SetSize(pixelWidth, pixelHeight);
          bitmapFrameEncode.SetPixelFormat(ref wicPixelFormat);

          // this is the trick to write D2D1 bitmap to WIC
          var imageEncoder = new ImageEncoder(GraphicsDevice11.Instance.FactoryWIC, GraphicsDevice11.Instance.Device2D1);
          imageEncoder.WriteFrame(bitmap, bitmapFrameEncode, new ImageParameters(bitmap.PixelFormat, 96, 96, 0, 0, pixelWidth, pixelHeight));

          bitmapFrameEncode.Commit();
        }
        encoder.Commit();
      }
    }
  }
}
