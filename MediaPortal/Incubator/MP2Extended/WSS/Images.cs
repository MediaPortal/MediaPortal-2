#region Copyright (C) 2011-2013 MPExtended

// Copyright (C) 2011-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.MP2Extended.WSS
{
  internal static class Images
  {
    private static string GetMime(string extension)
    {
      string lowerExtension = (extension.StartsWith(".") ? extension.Substring(1) : extension).ToLower();
      Dictionary<string, string> commonMimeTypes = new Dictionary<string, string>()
      {
        { "jpeg", "image/jpeg" },
        { "jpg", "image/jpeg" },
        { "png", "image/png" },
        { "gif", "image/gif" },
        { "bmp", "image/x-ms-bmp" },
      };
      return commonMimeTypes.ContainsKey(lowerExtension) ? commonMimeTypes[lowerExtension] : "application/octet-stream";
    }

    internal static byte[] ResizeImage(byte[] inputBytes, int? maxWidth, int? maxHeight, string borders)
    {
      Stream stream = new MemoryStream(inputBytes);
      using (var origImage = Image.FromStream(stream))
      {
        // newImageSize is the size of the actual graphic, which might not be the size of the canvas (bitmap). Unless
        // we're instructed to stretch the image, it's a proportional resize of the source graphic. Borders will be
        // added when we're instructed to and the aspect ratio of the image isn't equal to the aspect ratio of the 
        // bitmap.
        Resolution newImageSize = borders != "stretch" ?
          Resolution.Calculate(origImage.Width, origImage.Height, maxWidth, maxHeight, 1) :
          Resolution.Create(maxWidth.Value, maxHeight.Value);
        bool addBorders = !String.IsNullOrEmpty(borders) && borders != "stretch" && newImageSize.AspectRatio != (double)maxWidth / maxHeight;

        Resolution bitmapSize = addBorders ? Resolution.Create(maxWidth.Value, maxHeight.Value) : newImageSize;
        Bitmap newImage = new Bitmap(bitmapSize.Width, bitmapSize.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphic = Graphics.FromImage(newImage))
        {
          graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
          graphic.SmoothingMode = SmoothingMode.HighQuality;
          graphic.CompositingQuality = CompositingQuality.HighQuality;
          graphic.CompositingMode = CompositingMode.SourceCopy;
          graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

          if (addBorders && borders != "transparent")
            graphic.FillRectangle(new SolidBrush(ColorTranslator.FromHtml("#" + borders)), 0, 0, bitmapSize.Width, bitmapSize.Height);

          // We center the graphic in the canvas. If we should stretch the image, newImageSize is equal to the canvas, so 
          // the graphic is pasted at the top-left corner, which is fine.
          int leftOffset = (bitmapSize.Width - newImageSize.Width) / 2;
          int heightOffset = (bitmapSize.Height - newImageSize.Height) / 2;
          graphic.DrawImage(origImage, leftOffset, heightOffset, newImageSize.Width, newImageSize.Height);
        }

        return ImageToByteArray(newImage, origImage.RawFormat);
      }
    }

    public static byte[] ImageToByteArray(Image imageIn, ImageFormat format)
    {
      MemoryStream ms = new MemoryStream();
      imageIn.Save(ms, format);
      return ms.ToArray();
    }


    private static void SaveImageToFile(Image image, string path, string format)
    {
      switch (format.ToLower())
      {
        case "png":
          image.Save(path, ImageFormat.Png);
          break;

        case "gif":
          image.Save(path, ImageFormat.Gif);
          break;

        case "bmp":
          image.Save(path, ImageFormat.Bmp);
          break;

        case "jpeg":
        case "jpg":
          var jpegInfo = ImageCodecInfo.GetImageEncoders().First(enc => enc.FormatID == ImageFormat.Jpeg.Guid);
          var jpegParameters = new EncoderParameters(1);
          jpegParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
          image.Save(path, jpegInfo, jpegParameters);
          break;

        default:
          ServiceRegistration.Get<ILogger>().Warn("Requested invalid file format '{0}'", format);
          throw new ArgumentException(String.Format("Invalid file format '{0}'", format));
      }
    }
  }
}