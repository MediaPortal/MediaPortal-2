using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;

namespace Skinengine.Controls.Brushes
{
  public class ImageBrush : TileBrush
  {
    Property _imageSourceProperty;
    Property _downloadProgressProperty;
    public ImageBrush()
    {
      _imageSourceProperty = new Property(null);
      _downloadProgressProperty = new Property((double)0.0f);
    }

    public Property ImageSourceProperty
    {
      get
      {
        return _imageSourceProperty;
      }
      set
      {
        _imageSourceProperty = value;
      }
    }

    public Uri ImageSource
    {
      get
      {
        return (Uri)_imageSourceProperty.GetValue();
      }
      set
      {
        _imageSourceProperty.SetValue(value);
      }
    }
    public Property DownloadProgressProperty
    {
      get
      {
        return _downloadProgressProperty;
      }
      set
      {
        _downloadProgressProperty = value;
      }
    }

    public double DownloadProgress
    {
      get
      {
        return (double)_downloadProgressProperty.GetValue();
      }
      set
      {
        _downloadProgressProperty.SetValue(value);
      }
    }
  }
}
