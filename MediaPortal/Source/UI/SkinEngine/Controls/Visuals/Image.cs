#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.UI.SkinEngine.MpfElements;
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
    #region Classes

    protected class ImageSourceState
    {
      protected ImageSource _imageSource = null;
      protected bool _imageSourceInvalid = true;
      protected bool _setup = false;

      public ImageSource ImageSource
      {
        get { return _imageSource; }
        set { _imageSource = value; }
      }

      public bool Setup
      {
        get { return _setup; }
        set { _setup = value; }
      }
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _fallbackSourceProperty;
    protected AbstractProperty _sourceProperty;
    protected AbstractProperty _stretchDirectionProperty;
    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _thumbnailProperty;
    protected AbstractProperty _skinNeutralProperty;
    protected AbstractProperty _hasImageProperty;
    protected readonly ImageSourceState _sourceState = new ImageSourceState();
    protected readonly ImageSourceState _fallbackSourceState = new ImageSourceState();
    protected bool _loadImageSource = false;
    protected bool _fallbackSourceInUse = false;
    protected SizeF _lastImageSourceSize = SizeF.Empty;
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
      _skinNeutralProperty = new SProperty(typeof(bool), false);
      _hasImageProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _sourceProperty.Attach(OnSourceChanged);
      _fallbackSourceProperty.Attach(OnFallbackSourceChanged);
      _stretchProperty.Attach(OnArrangeGetsInvalid);
      _stretchDirectionProperty.Attach(OnArrangeGetsInvalid);
      _thumbnailProperty.Attach(OnArrangeGetsInvalid);
      _skinNeutralProperty.Attach(OnArrangeGetsInvalid);
      _widthProperty.Attach(OnImageSizeChanged);
      _heightProperty.Attach(OnImageSizeChanged);
    }

    void Detach()
    {
      _sourceProperty.Detach(OnSourceChanged);
      _fallbackSourceProperty.Detach(OnFallbackSourceChanged);
      _stretchProperty.Detach(OnArrangeGetsInvalid);
      _stretchDirectionProperty.Detach(OnArrangeGetsInvalid);
      _thumbnailProperty.Detach(OnArrangeGetsInvalid);
      _skinNeutralProperty.Detach(OnArrangeGetsInvalid);
      _widthProperty.Detach(OnImageSizeChanged);
      _heightProperty.Detach(OnImageSizeChanged);
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
      SkinNeutralAR = i.SkinNeutralAR;
      DisposeImageSources();
      _loadImageSource = true;
      HasImage = false;
      Attach();
    }

    public override void Dispose()
    {
      DisposeImageSources();
      base.Dispose();
    }

    #endregion

    void OnSourceChanged(AbstractProperty property, object oldValue)
    {
      InvalidateImageSources();
    }

    void OnFallbackSourceChanged(AbstractProperty property, object oldValue)
    {
      if (!_fallbackSourceInUse)
        return;
      InvalidateImageSources();
    }

    protected void InvalidateImageSources()
    {
      _loadImageSource = true;
      InvalidateLayout(true, false);
    }

    protected void DisposeImageSources()
    {
      Registration.TryCleanupAndDispose(_sourceState.ImageSource);
      _sourceState.ImageSource = null;
      Registration.TryCleanupAndDispose(_fallbackSourceState.ImageSource);
      _fallbackSourceState.ImageSource = null;
    }

    /// <summary>
    /// Gets or sets the <see cref="Stretch"/> mode that will be used to fit the <see cref="ImageSource"/> into the control.
    /// </summary>
    public Stretch Stretch
    {
      get { return (Stretch) _stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public AbstractProperty StretchProperty
    {
      get { return _stretchProperty; }
    }

    /// <summary>
    /// Gets or sets a whether the <see cref="ImageSource"/> will be scaled up, down or either way top fit the control.
    /// </summary>
    public StretchDirection StretchDirection
    {
      get { return (StretchDirection) _stretchDirectionProperty.GetValue(); }
      set { _stretchDirectionProperty.SetValue(value); }
    }

    public AbstractProperty StretchDirectionProperty
    {
      get { return _stretchDirectionProperty; }
    }

    /// <summary>
    /// Gets or sets the primary <see cref="ImageSource"/> to display.
    /// </summary>
    public object Source
    {
      get { return _sourceProperty.GetValue(); }
      set { _sourceProperty.SetValue(value); }
    }

    public AbstractProperty SourceProperty
    {
      get { return _sourceProperty; }
    }

    /// <summary>
    /// Gets of sets the backup <see cref="ImageSource"/> that will be used if the primary <see cref="Source"/> cannot be loaded.
    /// </summary>
    public object FallbackSource
    {
      get { return _fallbackSourceProperty.GetValue(); }
      set { _fallbackSourceProperty.SetValue(value); }
    }

    public AbstractProperty FallbackSourceProperty
    {
      get { return _fallbackSourceProperty; }
    }

    /// <summary>
    /// Gets or sets whether the <see cref="ImageSource"/> should be displayed as a thumbnail, which is a more efficient way of 
    /// displaying large images scaled down to small areas.
    /// </summary>
    public bool Thumbnail
    {
      get { return (bool) _thumbnailProperty.GetValue(); }
      set { _thumbnailProperty.SetValue(value); }
    }

    public AbstractProperty ThumbnailProperty
    {
      get { return _thumbnailProperty; }
    }

    /// <summary>
    /// Gets or sets a value that determines whther the skin AR is compensated for when calculating image stretching
    /// </summary>
    public bool SkinNeutralAR
    {
      get { return (bool) _skinNeutralProperty.GetValue(); }
      set { _skinNeutralProperty.SetValue(value); }
    }

    public AbstractProperty SkinNeutralARProperty
    {
      get { return _skinNeutralProperty; }
    }

    /// <summary>
    /// Gets a value indicating that the control has a renderable ImageSource
    /// </summary>
    public bool HasImage
    {
      get { return (bool) _hasImageProperty.GetValue(); }
      set { _hasImageProperty.SetValue(value); }
    }

    public AbstractProperty HasImageProperty
    {
      get { return _hasImageProperty; }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      ImageSourceState allocatedSource = GetLoadedSource(false);
      if (allocatedSource == null)
      {
        _lastImageSourceSize = SizeF.Empty;
        return new SizeF(10, 10);
      }

      SizeF imageSize = allocatedSource.ImageSource.SourceSize;
      float sourceFrameRatio = imageSize.Width / imageSize.Height;
      // Adaptions when available size is not specified in any direction(s)
      if (double.IsNaN(totalSize.Width) && double.IsNaN(totalSize.Height))
        totalSize = imageSize;
      else if (double.IsNaN(totalSize.Height))
        totalSize.Height = totalSize.Width / sourceFrameRatio;
      else if (double.IsNaN(totalSize.Width))
        totalSize.Width = totalSize.Height * sourceFrameRatio;

      _lastImageSourceSize = imageSize;
      return allocatedSource.ImageSource.StretchSource(totalSize, imageSize, Stretch, StretchDirection);
    }

    protected ImageSourceState GetLoadedSource(bool invalidateLayout)
    {
      if (_loadImageSource)
      {
        DisposeImageSources();
        _fallbackSourceInUse = false;
        _loadImageSource = false;
      }

      ImageSourceState allocatedSource = null;

      // Find a new image source
      if (_sourceState.ImageSource == null)
      {
        _sourceState.ImageSource = LoadImageSource(Source);
        _sourceState.Setup = false;
      }

      if (_sourceState.ImageSource != null && !_sourceState.ImageSource.IsAllocated)
      {
        _sourceState.Setup = false;
        _sourceState.ImageSource.Allocate();
      }

      if (_sourceState.ImageSource != null && _sourceState.ImageSource.IsAllocated)
        allocatedSource = _sourceState;
      else
      {
        // If the source image could not load yet, try fallback image
        if (_fallbackSourceState.ImageSource == null)
        {
          _fallbackSourceState.ImageSource = LoadImageSource(FallbackSource);
          _fallbackSourceState.Setup = false;
        }

        if (_fallbackSourceState.ImageSource != null && !_fallbackSourceState.ImageSource.IsAllocated)
        {
          _fallbackSourceState.Setup = false;
          _fallbackSourceState.ImageSource.Allocate();
        }

        if (_fallbackSourceState.ImageSource != null && _fallbackSourceState.ImageSource.IsAllocated)
        {
          _fallbackSourceInUse = true;
          allocatedSource = _fallbackSourceState;
        }
      }

      if (invalidateLayout && allocatedSource != null && allocatedSource.ImageSource.SourceSize != _lastImageSourceSize)
        InvalidateLayout(true, true);
      HasImage = allocatedSource != null;

      return allocatedSource;
    }

    protected bool IsValidSource(string uriSource)
    {
      string lower = uriSource.ToLower();
      if (lower.EndsWith(".png") || lower.EndsWith(".bmp") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg"))
        return true;

      if (Thumbnail && (lower.EndsWith(".avi") || lower.EndsWith(".ts") || lower.EndsWith(".mkv")))
        return true;

      return false;
    }

    // FIXME: Remove this ugly hack and find a general solution to make image sources react to size changes
    protected void OnImageSizeChanged(AbstractProperty prop, object oldValue)
    {
      // Invalidate the loaded sources for MediaItems to allow use of different thumb resolutions.
      if (_sourceState.ImageSource is MediaItemSource)
        InvalidateImageSources();
    }

    protected ImageSource LoadImageSource(object source)
    {
      ImageSource result;
      MediaItem mi = source as MediaItem;
      if (mi != null)
        result = new MediaItemSource(mi, (int) Math.Max(Width, Height));
      else
        result = source as ImageSource;
      if (result == null)
      {
        string uriSource = source as string;
        if (!string.IsNullOrEmpty(uriSource))
        {
          // Remember to adapt list of supported extensions for picture player plugin...
          if (IsValidSource(uriSource))
          {
            BitmapImage bmi = new BitmapImage {UriSource = uriSource, Thumbnail = Thumbnail};
            if (Thumbnail)
              // Set the requested thumbnail dimension, to use the best matching format.
              bmi.ThumbnailDimension = (int)Math.Max(Width, Height);
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
      return result;
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      ImageSourceState allocatedSource = GetLoadedSource(true);
      if (allocatedSource == null)
        base.DoRender(localRenderContext);
      else
      {
        // Update source geometry if necessary (source has changed, layout has changed).
        if (!allocatedSource.Setup)
        {
          allocatedSource.ImageSource.Setup(_innerRect, localRenderContext.ZOrder, SkinNeutralAR);
          allocatedSource.Setup = true;
        }
        base.DoRender(localRenderContext);
        allocatedSource.ImageSource.Render(localRenderContext, Stretch, StretchDirection);
      }
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      _sourceState.Setup = false;
      _fallbackSourceState.Setup = false;
    }

    public override void Allocate()
    {
      base.Allocate();
      GetLoadedSource(true);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (_sourceState.ImageSource != null)
      {
        _sourceState.ImageSource.Deallocate();
        _sourceState.ImageSource = null;
      }
      if (_fallbackSourceState.ImageSource != null)
      {
        _fallbackSourceState.ImageSource.Deallocate();
        _fallbackSourceState.ImageSource = null;
      }
    }
  }
}
