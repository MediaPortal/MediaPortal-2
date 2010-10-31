#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum StretchDirection { UpOnly, DownOnly, Both };

  public enum Stretch
  {
    // The content preserves its original size.
    None,

    // The content is resized to fill the destination dimensions. The aspect ratio is not preserved.
    Fill,

    // The content is resized to fit in the destination dimensions while it preserves its
    // native aspect ratio. If the aspect ratio of the destination rectangle differs from
    // the source, the content won't fill the whole destionation area.
    Uniform,

    // The content is resized to fill the destination dimensions while it preserves its
    // native aspect ratio. 
    // If the aspect ratio of the destination rectangle differs from the source, the source content is 
    // clipped to fit in the destination dimensions completely.
    UniformToFill
  }

  public class Image : Control
  {
    #region Protected fields

    protected AbstractProperty _fallbackSourceProperty;
    protected AbstractProperty _sourceProperty;
    protected AbstractProperty _stretchDirectionProperty;
    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _thumbnailProperty;
    protected ImageSource _imageSource = null;
    protected bool _imageSourceInvalid = true;
    protected string _warnURI = null;

    #endregion

    #region Ctor

    public Image()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _sourceProperty = new SProperty(typeof(object), null);
      _fallbackSourceProperty = new SProperty(typeof(object), null);
      _stretchDirectionProperty = new SProperty(typeof(StretchDirection), StretchDirection.Both);
      _stretchProperty = new SProperty(typeof(Stretch), Stretch.None);
      _thumbnailProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _sourceProperty.Attach(OnSourceChanged);
      _fallbackSourceProperty.Attach(OnSourceChanged);
      _stretchProperty.Attach(OnLayoutChanged);
      _stretchDirectionProperty.Attach(OnLayoutChanged);
      _thumbnailProperty.Attach(OnLayoutChanged);
    }

    void Detach()
    {
      _sourceProperty.Detach(OnSourceChanged);
      _fallbackSourceProperty.Detach(OnSourceChanged);
      _stretchProperty.Detach(OnLayoutChanged);
      _stretchDirectionProperty.Detach(OnLayoutChanged);
      _thumbnailProperty.Detach(OnLayoutChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Image i = (Image) source;
      Source = i.Source;
      FallbackSource = i.FallbackSource;
      StretchDirection = i.StretchDirection;
      Stretch = i.Stretch;
      Thumbnail = i.Thumbnail;
      _imageSourceInvalid = true;
      Attach();
    }

    #endregion

    void OnLayoutChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
    }

    void OnSourceChanged(AbstractProperty property, object oldValue)
    {
      _imageSourceInvalid = true;
      InvalidateLayout();
    }

    public AbstractProperty StretchProperty
    {
      get { return _stretchProperty; }
    }

    public Stretch Stretch
    {
      get { return (Stretch) _stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public AbstractProperty SourceProperty
    {
      get { return _sourceProperty; }
    }

    public object Source
    {
      get { return _sourceProperty.GetValue(); }
      set { _sourceProperty.SetValue(value); }
    }

    public AbstractProperty FallbackSourceProperty
    {
      get { return _fallbackSourceProperty; }
    }

    public object FallbackSource
    {
      get { return _fallbackSourceProperty.GetValue(); }
      set { _fallbackSourceProperty.SetValue(value); }
    }

    public AbstractProperty StretchDirectionProperty
    {
      get { return _stretchDirectionProperty; }
    }

    public StretchDirection StretchDirection
    {
      get { return (StretchDirection) _stretchDirectionProperty.GetValue(); }
      set { _stretchDirectionProperty.SetValue(value); }
    }

    public AbstractProperty ThumbnailProperty
    {
      get { return _thumbnailProperty; }
    }

    public bool Thumbnail
    {
      get { return (bool) _thumbnailProperty.GetValue(); }
      set { _thumbnailProperty.SetValue(value); }
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      ImageSource source = GetLoadedSource();
      if (source == null || !source.IsAllocated)
        return SizeF.Empty;

      SizeF imageSize = source.SourceSize;
      float sourceFrameRatio = imageSize.Width / imageSize.Height;
      // Adaptions when available size is not specified in any direction(s)
      if (double.IsNaN(totalSize.Width) && double.IsNaN(totalSize.Height))
        totalSize = imageSize;
      else if (double.IsNaN(totalSize.Height))
        totalSize.Height = totalSize.Width / sourceFrameRatio;
      else if (double.IsNaN(totalSize.Width))
        totalSize.Width = totalSize.Height * sourceFrameRatio;

      return source.StretchSource(totalSize, imageSize, Stretch, StretchDirection);
    }

    protected ImageSource GetLoadedSource()
    {
      if (_imageSource != null && !_imageSourceInvalid)
        return _imageSource;
      if (_imageSource != null && _imageSource != Source && _imageSource != FallbackSource)
      {
        _imageSource.Deallocate();
        _imageSource.Dispose();
      }

      _imageSource = LoadImageSource(Source);
      if (_imageSource != null)
        return _imageSource;
      _imageSource = LoadImageSource(FallbackSource);
      return _imageSource;
    }

    protected ImageSource LoadImageSource(object source)
    {
      ImageSource result = source as ImageSource;
      if (result == null)
      {
        string uriSource = source as string;
        if (!string.IsNullOrEmpty(uriSource))
        {
          if (uriSource.ToLower().EndsWith(".png") || uriSource.ToLower().EndsWith(".bmp"))
          {
            BitmapImage bmi = new BitmapImage {UriSource = uriSource, Thumbnail = Thumbnail};
            result = bmi;
          }
          // TODO: More image types
          else
          {
            if (_warnURI != uriSource)
            {
              ServiceRegistration.Get<ILogger>().Warn("Image source '{0}' is not supported", uriSource);
              // Remember if we already wrote a warning to the log to avoid log flooding
              _warnURI = uriSource;
            }
          }
        }
      }
      if (result != null && !result.IsAllocated)
        result.Allocate();
      return result;
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);
      ImageSource source = GetLoadedSource();
      if (source != null)
        source.Render(localRenderContext, _innerRect, Stretch, StretchDirection);
    }

    public override void Allocate()
    {
      base.Allocate();
      GetLoadedSource();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      ImageSource imageSource = Source as ImageSource;
      if (imageSource != null)
        imageSource.Deallocate();
      imageSource = FallbackSource as ImageSource;
      if (imageSource != null)
        imageSource.Deallocate();
      if (_imageSource != null)
        _imageSource.Deallocate();
    }
  }
}
