#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MediaPortal.Utilities.Graphics
{
  /// <summary>
  /// Provides various image untilities, such as high quality resizing and the ability to save a JPEG.
  /// </summary>
  public static class ImageUtilities
  {
    /// <summary>
    /// A quick lookup for getting image encoders
    /// </summary>
    private static Dictionary<string, ImageCodecInfo> _encoders = null;

    /// <summary>
    /// A quick lookup for getting image encoders
    /// </summary>
    public static Dictionary<string, ImageCodecInfo> Encoders
    {
      // Get accessor that creates the dictionary on demand
      get
      {
        if (_encoders != null)
          return _encoders;
        // If the quick lookup isn't initialised, initialise it
        return _encoders = ImageCodecInfo.GetImageEncoders().ToDictionary(codec => codec.MimeType.ToLower());
      }
    }

    /// <summary>
    /// Resize the image to the specified width and height using high quality mode.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The width to resize to.</param>
    /// <param name="height">The height to resize to.</param>
    /// <returns>The resized image.</returns>
    public static Bitmap ResizeImageExact(Image image, int width, int height)
    {
      Bitmap result = new Bitmap(width, height);
      // Use a graphics object to draw the resized image into the bitmap
      using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(result))
      {
        // Set the resize quality modes to high quality
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        // Draw the image into the target bitmap
        graphics.DrawImage(image, 0, 0, result.Width, result.Height);
      }
      return result;
    }

    /// <summary>
    /// Resizes the given <paramref name="fullsizeImage"/> to a maximum <paramref name="maxWidth"/> and <paramref name="maxHeight"/> while preserving
    /// corrent aspect ratio. By default images are not upscaled, if you want this, set <paramref name="allowUpScale"/> to <c>true</c>.
    /// </summary>
    /// <param name="fullsizeImage">Image</param>
    /// <param name="maxWidth">Max. width</param>
    /// <param name="maxHeight">Max. height</param>
    /// <param name="allowUpScale">Allow upscaling</param>
    /// <returns>Resized image</returns>
    public static Image ResizeImage(Image fullsizeImage, int maxWidth, int maxHeight, bool allowUpScale = false)
    {
      if (!allowUpScale && fullsizeImage.Width <= maxWidth && fullsizeImage.Height <= maxHeight)
        return fullsizeImage;

      if (fullsizeImage.Width <= maxWidth)
        maxWidth = fullsizeImage.Width;

      int newHeight = fullsizeImage.Height * maxWidth / fullsizeImage.Width;
      if (newHeight > maxHeight)
      {
        // Resize with height instead
        maxWidth = fullsizeImage.Width * maxHeight / fullsizeImage.Height;
        newHeight = maxHeight;
      }

      using (fullsizeImage)
        return ResizeImageExact(fullsizeImage, maxWidth, newHeight);
    }

    /// <summary>
    /// Checks the given <paramref name="image"/> for contained EXIF orientation tags and automatically rotates the image if required.
    /// </summary>
    /// <param name="image">Image</param>
    public static void ExifAutoRotate(this Image image)
    {
      RotateFlipType rotate = RotateFlipType.RotateNoneFlipNone;
      var exifOrientation = image.PropertyItems.FirstOrDefault(p => p.Id == 0x0112);
      if (exifOrientation != null)
      {
        var value = (int)exifOrientation.Value[0];
        if (value == 6)
          rotate = RotateFlipType.Rotate90FlipNone;
        else if (value == 8)
          rotate = RotateFlipType.Rotate270FlipNone;
        else if (value == 3)
          rotate = RotateFlipType.Rotate180FlipNone;
      }

      if (rotate != RotateFlipType.RotateNoneFlipNone)
        image.RotateFlip(rotate);
    }

    /// <summary> 
    /// Saves an image as a jpeg image, with the given quality.
    /// </summary> 
    /// <param name="path">Path to which the image would be saved.</param>
    /// <param name="image">Image to save.</param>
    /// <param name="quality">An integer from <c>0</c> to <c>100</c>, with <c>100</c> being the highest quality</param> 
    /// <exception cref="ArgumentOutOfRangeException">
    /// An invalid value was entered for image quality.
    /// </exception>
    public static void SaveJpeg(string path, Image image, int quality)
    {
      // Ensure the quality is within the correct range
      if ((quality < 0) || (quality > 100))
      {
        string error = string.Format("Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality);
        throw new ArgumentOutOfRangeException(error);
      }

      // Create an encoder parameter for the image quality
      EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
      ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

      // Create a collection of all parameters that we will pass to the encoder
      EncoderParameters encoderParams = new EncoderParameters(1);
      encoderParams.Param[0] = qualityParam;
      image.Save(path, jpegCodec, encoderParams);
    }

    /// <summary> 
    /// Saves an image as a jpeg image into the given <paramref name="stream"/>, with the given quality.
    /// </summary> 
    /// <param name="stream">Stream to save the image into.</param>
    /// <param name="image">Image to save.</param>
    /// <param name="quality">An integer from <c>0</c> to <c>100</c>, with <c>100</c> being the highest quality</param> 
    /// <exception cref="ArgumentOutOfRangeException">
    /// An invalid value was entered for image quality.
    /// </exception>
    public static void SaveJpeg(Stream stream, Image image, int quality)
    {
      // Ensure the quality is within the correct range
      if ((quality < 0) || (quality > 100))
      {
        string error = string.Format("Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality);
        throw new ArgumentOutOfRangeException(error);
      }

      // Create an encoder parameter for the image quality
      EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
      ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

      // Create a collection of all parameters that we will pass to the encoder
      EncoderParameters encoderParams = new EncoderParameters(1);
      encoderParams.Param[0] = qualityParam;
      image.Save(stream, jpegCodec, encoderParams);
    }

    /// <summary> 
    /// Returns the image codec with the given mime type 
    /// </summary> 
    public static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
      // Do a case insensitive search for the mime type
      string lookupKey = mimeType.ToLower();

      ImageCodecInfo foundCodec;
      Encoders.TryGetValue(lookupKey, out foundCodec);
      return foundCodec;
    }

    /// <summary>
    /// Resizes a Image that is loaded from the given <paramref name="sourceStream"/> as binary image. The returned stream
    /// can be read directly from beginning. The caller must <see cref="IDisposable.Dispose"/>() the returned instances.
    /// </summary>
    /// <param name="sourceStream">Source stream</param>
    /// <param name="targetFormat">Format to save into target stream</param>
    /// <param name="maxWidth">Max. width</param>
    /// <param name="maxHeight">Max. height</param>
    /// <returns>Stream containing the resized image</returns>
    public static Stream ResizeImage(Stream sourceStream, ImageFormat targetFormat, int maxWidth, int maxHeight)
    {
      if (sourceStream.CanSeek && sourceStream.Length == 0)
        return null;
      using (Image bitmap = ResizeImage(Image.FromStream(sourceStream), maxWidth, maxHeight))
      {
        MemoryStream tmpImageStream = new MemoryStream();
        bitmap.Save(tmpImageStream, targetFormat);
        tmpImageStream.Position = 0;
        return tmpImageStream;
      }
    }
  }
}
