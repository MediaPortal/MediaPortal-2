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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//using System.Windows.Controls;

namespace SkinEngine.Thumbnails
{
  public class ThumbnailBuilder
  {
    #region subclasses

    #endregion

    /// <summary>
    /// Creates the thumbnail for a folder.
    /// </summary>
    /// <param name="thumb">The thumb.</param>
    /// <returns></returns>
    public bool CreateThumbnailForFolder(WorkItem thumb)
    {
      Trace.WriteLine(String.Format("create folder thumb:{0}->{1}", thumb.Source, thumb.Destination));
      try
      {
        string thumbnailFileName = thumb.Destination;
        if (File.Exists(thumbnailFileName))
        {
          return true;
        }

        //find media files within the folder

        Database dbFolder = DatabaseCache.Instance.Get(thumb.DestinationFolder);
        string[] subNails = new string[4];
        int currentThumb = 0;
        string[] files = Directory.GetFiles(thumb.SourceFolder);
        for (int i = 0; i < files.Length; ++i)
        {
          string ext = Path.GetExtension(files[i]).ToLower();
          if (ext == ".wmv" || ext == ".avi" || ext == ".mkv" || ext == ".dvr-ms" || ext == ".jpg" || ext == ".png" ||
              ext == ".ts")
          {
            //media file found, create a thumbnail for this media file
            string dest = Path.ChangeExtension(files[i], ".jpg");
            Trace.WriteLine(" create:" + dest);
            WorkItem sub = new WorkItem(files[i], dest, thumb.Width, thumb.Height, thumb.Quality);
            if (CreateThumbnailForFile(dbFolder, sub))
            {
              subNails[currentThumb] = sub.DestinationFile;
              currentThumb++;
            }
            if (currentThumb >= 4)
            {
              break;
            }
          }
        }

        //no media files found?
        if (currentThumb <= 0)
        {
          Trace.WriteLine("create folder thumb done, no files");
          return false;
        }

        //create folder thumb 
        RenderTargetBitmap rtb =
          new RenderTargetBitmap(thumb.Width, thumb.Height, 1/200, 1/200, PixelFormats.Pbgra32);
        DrawingVisual dv = new DrawingVisual();
        DrawingContext dc = dv.RenderOpen();
        for (int i = 0; i < currentThumb; ++i)
        {
          double width = ((thumb.Width - 20)/2);
          double height = ((thumb.Height - 20)/2);
          if (currentThumb == 2)
          {
            width = (thumb.Width - 20)/2;
            height = (thumb.Height - 20);
          }
          if (currentThumb == 1)
          {
            height = (thumb.Height - 20);
            width = (thumb.Width - 20);
          }
          Trace.WriteLine(" load:" + subNails[i]);
          BitmapDecoder decoder;
          using (MemoryStream stream = new MemoryStream(dbFolder.Get(subNails[i])))
          {
            if (Path.GetExtension(subNails[i]) == ".jpg")
            {
              decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            else
            {
              decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            BitmapFrame frame = decoder.Frames[0];
            Rect rect = new Rect();
            rect.X = (i%2)*width + 10;
            rect.Y = (i/2)*height + 10;
            rect.Width = width;
            rect.Height = height;
            dc.DrawRectangle(new ImageBrush(frame), null, rect);
          }
          //dc.DrawImage(frame, rect);
        }

        dc.Close();
        rtb.Render(dv);
        Trace.WriteLine(" encode:" + thumbnailFileName);
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.QualityLevel = thumb.Quality;
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using (MemoryStream stream = new MemoryStream())
        {
          encoder.Save(stream);
          dbFolder.Add("folder.jpg", stream);
        }
        Trace.WriteLine("create folder thumb done");
        return true;
      }
      catch (Exception ex)
      {
        Trace.WriteLine("create folder thumb exception");
        Trace.WriteLine(ex);
      }
      Trace.WriteLine("create folder thumb failed");
      return false;
    }

    /// <summary>
    /// Creates a thumbnail for file.
    /// </summary>
    /// <param name="thumb">The thumb.</param>
    /// <returns></returns>
    public bool CreateThumbnailForFile(WorkItem thumb)
    {
      Database dbs = DatabaseCache.Instance.Get(thumb.SourceFolder);
      bool result = CreateThumbnailForFile(dbs, thumb);
      return result;
    }

    public bool CreateThumbnailForFile(Database dbs, WorkItem thumb)
    {
      string ext = Path.GetExtension(thumb.Source).ToLower();
      try
      {
        if (File.Exists(thumb.Source))
        {
          if (!dbs.Contains(thumb.SourceFile))
          {
            Trace.WriteLine(String.Format("create thumb:{0}->{1}", thumb.Source, thumb.Destination));

            if (ext == ".wmv" || ext == ".avi" || ext == ".mkv" || ext == ".dvr-ms" || ext == ".ts")
            {
              //create thumb of movie
              try
              {
                MediaPlayer player = new MediaPlayer();
                player.Open(new Uri(thumb.Source, UriKind.Absolute));
                player.ScrubbingEnabled = true;
                player.Play();
                player.Pause();
                player.Position = new TimeSpan(0, 0, 1);
                Thread.Sleep(4000);
                RenderTargetBitmap rtb =
                  new RenderTargetBitmap(thumb.Width, thumb.Height, 1/200, 1/200, PixelFormats.Pbgra32);
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawVideo(player, new Rect(0, 0, thumb.Width, thumb.Height));
                dc.Close();
                rtb.Render(dv);
                BitmapEncoder encoder;
                if (Path.GetExtension(thumb.Destination) == ".png")
                {
                  encoder = new PngBitmapEncoder();
                }
                else
                {
                  encoder = new JpegBitmapEncoder();
                  ((JpegBitmapEncoder) encoder).QualityLevel = thumb.Quality;
                }
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                using (MemoryStream stream = new MemoryStream())
                {
                  encoder.Save(stream);
                  dbs.Add(thumb.SourceFile, stream);
                }
                player.Stop();

                GC.Collect();
                GC.Collect();
                GC.Collect();
                return true;
              }
              catch (Exception ex)
              {
                Trace.WriteLine("create file thumb exception using mediaplayer");
                Trace.WriteLine(ex);
              }
            } // of if ( ext == ".wmv" || ext == ".avi" || ext == ".mkv" || ext == ".dvr-ms")
            else if (ext == ".png" || ext == ".jpg")
            {
              BitmapImage myBitmapImage = new BitmapImage();

              myBitmapImage.CacheOption = BitmapCacheOption.None;
              myBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
              // BitmapImage.UriSource must be in a BeginInit/EndInit block
              myBitmapImage.BeginInit();
              myBitmapImage.UriSource = new Uri(thumb.Source);
              myBitmapImage.DecodePixelWidth = thumb.Width;
              myBitmapImage.EndInit();

              using (MemoryStream stream = new MemoryStream())
              {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = thumb.Quality;
                encoder.Frames.Add(BitmapFrame.Create(myBitmapImage));
                encoder.Save(stream);
                dbs.Add(thumb.SourceFile, stream);
              }
              return true;
            }
          } // of if (!dbs.Contains(thumb.SourceFile))
          else
          {
            return true;
          }
        } // of if (System.IO.File.Exists(thumb.Source))
      }
      catch (Exception ex)
      {
        Trace.WriteLine("create file thumb exception ");
        Trace.WriteLine(ex);
      }
      return false;
    }
  }
}
