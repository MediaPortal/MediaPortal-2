#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Services.ThumbnailGenerator.Database;
using MediaPortal.Thumbnails;

namespace MediaPortal.Services.ThumbnailGenerator
{
  public class ThumbnailBuilder
  {
    public static readonly ICollection<string> VALID_EXTENSIONS_MOVIES =
        new List<string>(new string[] { ".wmv", ".avi", ".mkv", ".dvr-ms", ".ts" });

    public static readonly ICollection<string> VALID_EXTENSIONS_IMAGES =
        new List<string>(new string[] { ".jpg", ".png" });

    protected static BitmapEncoder CreateBitmapEncoder(ImageType imageType, int quality)
    {
      BitmapEncoder result;
      switch (imageType)
      {
        case ImageType.Png:
          result = new PngBitmapEncoder();
          break;
        case ImageType.Jpeg:
          result = new JpegBitmapEncoder();
          ((JpegBitmapEncoder)result).QualityLevel = quality;
          break;
        default:
          throw new ArgumentException(string.Format("Thumbnail type '{0}' not supported", imageType));
      }
      return result;
    }

    /// <summary>
    /// Creates the thumbnail for a folder.
    /// </summary>
    /// <param name="folderPath">The directory path to build a thumbnail for.</param>
    /// <param name="thumbName">Name of the folder tumbnail to set in the
    /// <paramref name="thumbDb"/>.</param>
    /// <param name="width">The width of the thumbnail to create.</param>
    /// <param name="height">The height of the thumbnail to create.</param>
    /// <param name="quality">The quality of the thumbnail to create
    /// (Range: 1=lowest, ..., 100=highest).</param>
    /// <param name="imageType">Type of the image to create.</param>
    /// <param name="thumbDb">The thumbnail database for the folder.</param>
    /// <returns><c>true</c>, if the thumbnail could be successfully created, else <c>false</c>.</returns>
    public static bool CreateThumbnailForFolder(string folderPath, string thumbName,
        int width, int height, int quality, ImageType imageType, ThumbDatabase thumbDb)
    {
      try
      {
        // Find media files within the folder
        byte[][] subNails = new byte[4][];
        ImageType[] subNailTypes = new ImageType[4];
        int thumbCount = 0;
        foreach (string filePath in Directory.GetFiles(folderPath))
        {
          string ext = Path.GetExtension(filePath).ToLower();
          if (!VALID_EXTENSIONS_IMAGES.Contains(ext) && !VALID_EXTENSIONS_MOVIES.Contains(ext))
            continue;
          // Media file found, create a thumbnail for this media file
          byte[] subNail;
          ImageType subNailType;
          if (!thumbDb.Get(filePath, out subNail, out subNailType))
            if (CreateThumbnailForFile(filePath, width, height, quality, subNailType = imageType, thumbDb))
            {
              if (!thumbDb.Get(filePath, out subNail, out subNailType))
                subNail = null;
            }
            else
              subNail = null;
          if (subNail != null)
          {
            subNailTypes[thumbCount] = subNailType;
            subNails[thumbCount] = subNail;
            thumbCount++;
          }
          if (thumbCount >= 4)
            break;
        }

        // No media files found?
        if (thumbCount <= 0)
          return false;

        // Create folder thumb 
        RenderTargetBitmap rtb =
          new RenderTargetBitmap(width, height, 1 / (double) 200, 1 / (double) 200, PixelFormats.Pbgra32);
        DrawingVisual dv = new DrawingVisual();
        DrawingContext dc = dv.RenderOpen();
        for (int i = 0; i < thumbCount; i++)
        {
          int childWidth = (width - 20)/2;
          int childHeight = (height - 20)/2;
          if (thumbCount == 2)
          {
            childWidth = (width - 20)/2;
            childHeight = height - 20;
          }
          if (thumbCount == 1)
          {
            childHeight = height - 20;
            childWidth = width - 20;
          }
          // Trace.WriteLine(" Load: " + subNails[i]);
          using (MemoryStream stream = new MemoryStream(subNails[i]))
          {
            BitmapDecoder decoder;
            if (subNailTypes[i] == ImageType.Jpeg)
              decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            else
              decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            BitmapFrame frame = decoder.Frames[0];
            Rect rect = new Rect();
            rect.X = (i%2)*childWidth + 10;
            rect.Y = (i/2)*childHeight + 10;
            rect.Width = childWidth;
            rect.Height = childHeight;
            dc.DrawRectangle(new ImageBrush(frame), null, rect);
          }
        }

        dc.Close();
        rtb.Render(dv);
        BitmapEncoder encoder = CreateBitmapEncoder(imageType, quality);
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using (MemoryStream stream = new MemoryStream())
        {
          encoder.Save(stream);
          thumbDb.Add(thumbName, imageType, stream);
        }
        // Trace.WriteLine("Create folder thumb done");
        return true;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Error creating thumbnail for folder '{0}'", ex, folderPath);
      }
      // Trace.WriteLine("Create folder thumb failed");
      return false;
    }

    /// <summary>
    /// Creates a thumbnail for a file.
    /// </summary>
    /// <param name="filePath">The path of the file to create the thumbnail for.</param>
    /// <param name="width">The width of the thumbnail to create.</param>
    /// <param name="height">The height of the thumbnail to create.</param>
    /// <param name="quality">The quality of the thumbnail to create
    /// (Range: 1=lowest, 100=highest).</param>
    /// <param name="imageType">Type of the image to create.</param>
    /// <param name="thumbDb">The database to store the thumbnail.</param>
    /// <returns><c>true</c>, if the thumbnail could be successfully created, else <c>false</c>.</returns>
    public static bool CreateThumbnailForFile(string filePath,
        int width, int height, int quality, ImageType imageType, ThumbDatabase thumbDb)
    {
      if (!File.Exists(filePath))
        return false;
      //Trace.WriteLine(String.Format("Create thumb: {0}->{1}", file.FullName, destination));

      string ext = Path.GetExtension(filePath).ToLower();
      try
      {
        if (VALID_EXTENSIONS_MOVIES.Contains(ext))
        {
          // Create thumb for movie
          MediaPlayer player = new MediaPlayer();
          player.Open(new Uri(filePath, UriKind.Absolute));
          player.ScrubbingEnabled = true;
          player.Play();
          player.Pause();
          player.Position = new TimeSpan(0, 0, 1);
          Thread.Sleep(4000);
          RenderTargetBitmap rtb =
              new RenderTargetBitmap(width, height, 1 / (double) 200, 1 / (double) 200, PixelFormats.Pbgra32);
          DrawingVisual dv = new DrawingVisual();
          DrawingContext dc = dv.RenderOpen();
          dc.DrawVideo(player, new Rect(0, 0, width, height));
          dc.Close();
          rtb.Render(dv);
          BitmapEncoder encoder = CreateBitmapEncoder(imageType, quality);
          encoder.Frames.Add(BitmapFrame.Create(rtb));
          using (MemoryStream stream = new MemoryStream())
          {
            encoder.Save(stream);
            thumbDb.Add(filePath, imageType, stream);
          }
          player.Stop();

          GC.Collect();
          GC.Collect();
          GC.Collect();
          return true;
        }
        else if (VALID_EXTENSIONS_IMAGES.Contains(ext))
        {
          BitmapImage myBitmapImage = new BitmapImage();

          myBitmapImage.CacheOption = BitmapCacheOption.None;
          myBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
          // BitmapImage.UriSource must be in a BeginInit/EndInit block
          myBitmapImage.BeginInit();
          myBitmapImage.UriSource = new Uri(filePath);
          myBitmapImage.DecodePixelWidth = width;
          myBitmapImage.EndInit();

          using (MemoryStream stream = new MemoryStream())
          {
            BitmapEncoder encoder = CreateBitmapEncoder(imageType, quality);
            encoder.Frames.Add(BitmapFrame.Create(myBitmapImage));
            encoder.Save(stream);
            thumbDb.Add(filePath, imageType, stream);
          }
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Error creating thumbnail for file '{0}'", ex, filePath);
      }
      return false;
    }
  }
}
