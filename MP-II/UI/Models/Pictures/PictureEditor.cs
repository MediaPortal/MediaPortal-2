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
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screen;
using Models.Pictures.Utilities;
using Models.Pictures.PixelOperations;

namespace Models.Pictures
{
  public class PictureEditor
  {
    Rectangle _croppingRect;
    Property _currentPicture;
    SlideShow _slideShow;
    Size _scaledImageSize;
    IPixelOperation _pixelOp;
    RotateFlipType _rotation = RotateFlipType.RotateNoneFlipNone;
    Property _croppingEnabled;
    bool _enableAutoLevel = false;
    public PictureEditor(SlideShow slideShow)
    {
      _slideShow = slideShow;
      _currentPicture = new Property(typeof(string), "");
      _croppingEnabled = new Property(typeof(bool), false);
    }

    #region properties
    public bool CroppingEnabled
    {
      get
      {
        return (bool)_croppingEnabled.GetValue();
      }
      set
      {
        _croppingEnabled.SetValue(value);
      }
    }
    public Property CroppingEnabledProperty
    {
      get
      {
        return _croppingEnabled;
      }
      set
      {
        _croppingEnabled = value;
      }
    }
    #endregion

    #region commands
    public void MoveLeft()
    {
      if (_croppingRect.X > 0)
      {
        _croppingRect.X -= 10;
        if (_croppingRect.X < 0)
          _croppingRect.X = 0;
        TransformImage();
      }
    }
    public void MoveRight()
    {
      if (_croppingRect.Right + 10 < _scaledImageSize.Width)
      {
        _croppingRect.X += 10;
        if (_croppingRect.Right >= _scaledImageSize.Width)
        {
          _croppingRect.X = (_scaledImageSize.Width - _croppingRect.Width);
        }
        TransformImage();
      }
    }
    public void MoveUp()
    {
      if (_croppingRect.Y > 0)
      {
        _croppingRect.Y -= 10;
        if (_croppingRect.Y < 0)
          _croppingRect.Y = 0;
        TransformImage();
      }
    }
    public void MoveDown()
    {
      if (_croppingRect.Bottom < _scaledImageSize.Height)
      {
        _croppingRect.Y += 10;
        if (_croppingRect.Bottom >= _scaledImageSize.Height)
        {
          _croppingRect.Y = (_scaledImageSize.Height - _croppingRect.Height);
        }
        TransformImage();
      }
    }
    public void ZoomOut()
    {
      float w = (float)_croppingRect.Width;
      float h = (float)_croppingRect.Height;
      float dx = w;
      float dy = h;
      w *= 0.9f;
      h *= 0.9f;
      dx -= w; dx /= 2.0f;
      dy -= h; dy /= 2.0f;

      _croppingRect.Width = (int)w;
      _croppingRect.Height = (int)h;
      _croppingRect.X += (int)(dx);
      _croppingRect.Y += (int)dy;
      TransformImage();
    }
    public void ZoomIn()
    {
      float w = (float)_croppingRect.Width;
      float h = (float)_croppingRect.Height;
      float dx = w;
      float dy = h;
      w *= 1.1f;
      h *= 1.1f;
      dx = w - dx; dx /= 2.0f;
      dy = h - dy; dy /= 2.0f;
      _croppingRect.X -= (int)(dx);
      _croppingRect.Y -= (int)dy;
      _croppingRect.Width = (int)w;
      _croppingRect.Height = (int)h;
      if (_croppingRect.Right > _scaledImageSize.Width)
        _croppingRect.Width = (_scaledImageSize.Width) - _croppingRect.X;

      if (_croppingRect.Bottom > _scaledImageSize.Height)
        _croppingRect.Height = (_scaledImageSize.Height) - _croppingRect.Y;
      TransformImage();
    }
    public void RotateLeft()
    {
      switch (_rotation)
      {
        case RotateFlipType.RotateNoneFlipNone:
          _rotation = RotateFlipType.Rotate270FlipNone;
          break;
        case RotateFlipType.Rotate270FlipNone:
          _rotation = RotateFlipType.Rotate180FlipNone;
          break;
        case RotateFlipType.Rotate180FlipNone:
          _rotation = RotateFlipType.Rotate90FlipNone;
          break;
        case RotateFlipType.Rotate90FlipNone:
          _rotation = RotateFlipType.RotateNoneFlipNone;
          break;
      }
      _pixelOp = null;
      _croppingRect = new Rectangle(0, 0, 0, 0);
      TransformImage();
    }

    public void RotateRight()
    {
      switch (_rotation)
      {
        case RotateFlipType.RotateNoneFlipNone:
          _rotation = RotateFlipType.Rotate90FlipNone;
          break;
        case RotateFlipType.Rotate90FlipNone:
          _rotation = RotateFlipType.Rotate180FlipNone;
          break;
        case RotateFlipType.Rotate180FlipNone:
          _rotation = RotateFlipType.Rotate270FlipNone;
          break;
        case RotateFlipType.Rotate270FlipNone:
          _rotation = RotateFlipType.RotateNoneFlipNone;
          break;
      }
      _pixelOp = null;
      _croppingRect = new Rectangle(0, 0, 0, 0);
      TransformImage();
    }

    public void SaveImage()
    {
      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = true;
      Uri currentPicture = _slideShow.CurrentPictureUri;
      if (currentPicture == null) return;
      if (currentPicture.IsFile == false) return;
      Rectangle crop = _croppingRect;

      Bitmap imgSource = (Bitmap)Bitmap.FromFile(currentPicture.LocalPath);
      PropertyItem[] props = imgSource.PropertyItems;
      if (_rotation != RotateFlipType.RotateNoneFlipNone)
      {
        imgSource.RotateFlip(_rotation);
      }
      float dx = ((float)imgSource.Width) / ((float)_scaledImageSize.Width);
      float dy = ((float)imgSource.Height) / ((float)_scaledImageSize.Height);

      crop.X = (int)(((float)crop.X) * dx);
      crop.Y = (int)(((float)crop.Y) * dy);

      crop.Width = (int)(((float)crop.Width) * dx);
      crop.Height = (int)(((float)crop.Height) * dy);

      using (Bitmap imgAutoLeveled = AutoLevel(imgSource))
      {
        using (Bitmap destImage = Crop(imgAutoLeveled, crop, false))
        {
          DoPixelOperation(destImage, _pixelOp);

          imgSource.Dispose();
          imgSource = null;
          for (int i = 0; i < props.Length; ++i)
            destImage.SetPropertyItem(props[i]);
          Save(destImage, currentPicture.LocalPath);
        }
      }

      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate("contentmanager");
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = "changed";
      msg.MessageData["fullpath"] = currentPicture.LocalPath;
      queue.Send(msg);
      //ServiceScope.Get<IScreenManager>().CurrentWindow.WaitCursorVisible = false;
      ServiceScope.Get<IScreenManager>().ShowPreviousScreen();

    }
    #endregion

    public void DisableCropping()
    {
      _croppingRect = new Rectangle(0, 0, 0, 0);
      CroppingEnabled = false;
      TransformImage();
    }
    public void EnableCropping()
    {
      CroppingEnabled = true;
      int xoff = _scaledImageSize.Width / 5;
      int yoff = _scaledImageSize.Height / 5;

      _croppingRect = new Rectangle(xoff, yoff, _scaledImageSize.Width - 2 * xoff, _scaledImageSize.Height - 2 * yoff);

    }
    public void DisableRedEye()
    {
      _pixelOp = null;
      TransformImage();
    }
    public void EnableRedEye()
    {
      _pixelOp = new RedEye(70, 90);
      TransformImage();
    }
    public void DisableContrast()
    {
      _enableAutoLevel = false;
      TransformImage();
    }
    public void EnableContrast()
    {
      _enableAutoLevel = true;
      TransformImage();
    }

    public void TransformImage()
    {
      Uri currentPicture = _slideShow.CurrentPictureUri;
      if (currentPicture == null) return;
      if (currentPicture.IsFile == false) return;
      ProcessImage(currentPicture.LocalPath, _pixelOp);
      CurrentPicture = System.IO.Path.GetFullPath("temp.jpg");
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate("contentmanager");
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = "changed";
      msg.MessageData["fullpath"] = CurrentPicture;
      queue.Send(msg);
    }

    public void RemoveRedEye()
    {
      _pixelOp = new RedEye(70, 90);
      TransformImage();
    }

    public void Reset()
    {
      _slideShow.UpdateCurrentPicture();
      _pixelOp = null;
      _rotation = RotateFlipType.RotateNoneFlipNone;
      _enableAutoLevel = false;
      _croppingRect = new Rectangle(0, 0, 0, 0);
      CroppingEnabled = false;
      TransformImage();
    }
    /// <summary>
    /// Gets or sets the current picture URI property.
    /// </summary>
    /// <value>The current picture URI property.</value>
    public Property CurrentPictureProperty
    {
      get
      {
        return _currentPicture;
      }
      set { _currentPicture = value; }
    }

    /// <summary>
    /// Gets or sets the current picture URI.
    /// </summary>
    /// <value>The current picture URI.</value>
    public string CurrentPicture
    {
      get
      {
        return (string)_currentPicture.GetValue();
      }
      set
      {
        _currentPicture.SetValue(value);
      }
    }

    void ProcessImage(string filename, IPixelOperation pixelOp)
    {
      using (Bitmap imgSource = (Bitmap)Bitmap.FromFile(filename))
      {
        if (_rotation != RotateFlipType.RotateNoneFlipNone)
        {
          imgSource.RotateFlip(_rotation);
        }

        float percent = 400.0f / ((float)imgSource.Width);
        if (imgSource.Height > imgSource.Width)
          percent = 400.0f / ((float)imgSource.Height);


        using (Bitmap scaledSource = ScaleByPercent(imgSource, (float)(100.0f * percent)))
        {
          using (Bitmap imgAutoLeveled = AutoLevel(scaledSource))
          {
            _scaledImageSize = scaledSource.Size;
            if (_croppingRect.IsEmpty)
              _croppingRect = new Rectangle(0, 0, _scaledImageSize.Width, _scaledImageSize.Height);
            using (Bitmap destImage = Crop(imgAutoLeveled, _croppingRect, true))
            {
              DoPixelOperation(destImage, pixelOp);
              Save(destImage, "temp.jpg");
            }
          }
        }
      }
    }
    Bitmap AutoLevel(Bitmap bmpPhoto)
    {
      if (!_enableAutoLevel) return bmpPhoto;
      HistogramRgb histogram = new HistogramRgb(3, 256);
      histogram.AddBitmap(bmpPhoto);
      Level levels = histogram.MakeLevelsAuto();
      if (levels.isValid)
      {
        levels.Apply(bmpPhoto);
      }
      return bmpPhoto;
    }

    void Save(Image img, string fileName)
    {
      if (File.Exists(fileName))
      {
        File.Delete(fileName);
      }
      img.Save(fileName, ImageFormat.Jpeg);
    }

    Bitmap ScaleByPercent(Bitmap imgPhoto, float percent)
    {
      float nPercent = ((float)percent / 100.0f);

      float sourceWidth = (float)imgPhoto.Width;
      float sourceHeight = (float)imgPhoto.Height;
      int sourceX = 0;
      int sourceY = 0;

      int destX = 0;
      int destY = 0;
      float destWidth;
      float destHeight;
      float ar = sourceWidth / sourceHeight;
      if (sourceWidth > sourceHeight)
      {
        destWidth = sourceWidth * nPercent;
        destHeight = destWidth / ar;
      }
      else
      {
        destHeight = sourceHeight * nPercent;
        destWidth = destHeight * ar;
      }


      Bitmap bmPhoto = new Bitmap((int)destWidth, (int)destHeight, PixelFormat.Format24bppRgb);
      //bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

      using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
      {
        grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(imgPhoto,
            new Rectangle(destX, destY, (int)destWidth, (int)destHeight),
            new Rectangle(sourceX, sourceY, (int)sourceWidth, (int)sourceHeight),
            GraphicsUnit.Pixel);

      }
      return bmPhoto;
    }

    Bitmap Crop(Bitmap imgPhoto, Rectangle cropRect, bool showMask)
    {
      if (showMask)
      {
        int sourceWidth = imgPhoto.Width;
        int sourceHeight = imgPhoto.Height;
        Bitmap bmPhoto = new Bitmap(sourceWidth, sourceHeight, PixelFormat.Format24bppRgb);
        bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

        using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
        {
          grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;


          grPhoto.DrawImage(imgPhoto, new Rectangle(0, 0, sourceWidth, sourceHeight), new Rectangle(0, 0, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
          grPhoto.FillRectangle(new SolidBrush(Color.FromArgb(128, 0, 0, 255)), new Rectangle(0, 0, sourceWidth, sourceHeight));

          grPhoto.DrawImage(imgPhoto,
              cropRect, //destination rect
              cropRect, //source rect
              GraphicsUnit.Pixel);

        }
        return bmPhoto;
      }
      else
      {
        Bitmap bmPhoto = new Bitmap(cropRect.Width, cropRect.Height, PixelFormat.Format24bppRgb);
        bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

        using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
        {
          grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
          grPhoto.DrawImage(imgPhoto,
              new Rectangle(0, 0, cropRect.Width, cropRect.Height), //destination rect
              cropRect, //source rect
              GraphicsUnit.Pixel);

        }
        return bmPhoto;
      }
    }

    void DoPixelOperation(Bitmap b, IPixelOperation pixelOp)
    {
      if (pixelOp == null)
        return;
      // GDI+ still lies to us - the return format is BGR, NOT RGB. 
      BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

      int stride = bmData.Stride;
      System.IntPtr Scan0 = bmData.Scan0;
      byte red, green, blue;
      unsafe
      {
        byte* p = (byte*)(void*)Scan0;
        int nOffset = stride - b.Width * 3;
        int nWidth = b.Width * 3;
        for (int y = 0; y < b.Height; ++y)
        {
          for (int x = 0; x < b.Width; ++x)
          {
            blue = p[0];
            green = p[1];
            red = p[2];
            Color c = Color.FromArgb(red, green, blue);
            c = pixelOp.ProcessPixel(c);

            p[0] = (byte)c.B;
            p[1] = (byte)c.G;
            p[2] = (byte)c.R;
            p += 3;
          }
          p += nOffset;
        }
      }

      b.UnlockBits(bmData);

    }
  }
}
