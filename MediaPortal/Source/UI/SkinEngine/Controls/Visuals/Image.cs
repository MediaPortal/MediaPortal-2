#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UI.SkinEngine.Utils;
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

  // TODO: Thumbnail property & handling is not implemented for all image sources. Makes it sense to have this property in the Image class? Should it be reworked?
  public class Image : FrameworkElement
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
    protected string _formerWarnURI = null;
    protected bool _invalidateImageSourceOnResize = false;

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
      _stretchProperty = new SProperty(typeof(Stretch), Stretch.None);
      _stretchDirectionProperty = new SProperty(typeof(StretchDirection), StretchDirection.Both);
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

      WidthProperty.Attach(OnImageSizeChanged);
      HeightProperty.Attach(OnImageSizeChanged);
    }

    void Detach()
    {
      _sourceProperty.Detach(OnSourceChanged);
      _fallbackSourceProperty.Detach(OnFallbackSourceChanged);
      _stretchProperty.Detach(OnArrangeGetsInvalid);
      _stretchDirectionProperty.Detach(OnArrangeGetsInvalid);
      _thumbnailProperty.Detach(OnArrangeGetsInvalid);
      _skinNeutralProperty.Detach(OnArrangeGetsInvalid);

      WidthProperty.Detach(OnImageSizeChanged);
      HeightProperty.Detach(OnImageSizeChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Image i = (Image) source;
      Source = copyManager.GetCopy(i.Source);
      FallbackSource = copyManager.GetCopy(i.FallbackSource);
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
      MPF.TryCleanupAndDispose(_sourceState.ImageSource);
      _sourceState.ImageSource = null;
      MPF.TryCleanupAndDispose(_fallbackSourceState.ImageSource);
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
        _sourceState.ImageSource = LoadImageSource(Source, true);
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
          _fallbackSourceState.ImageSource = LoadImageSource(FallbackSource, false);
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
      if (_invalidateImageSourceOnResize)
        // Invalidate the loaded sources for MediaItems to allow use of different thumb resolutions.
        InvalidateImageSources();
    }

    /// <summary>
    /// Loads an ImageSource and allows control of thumbnail use. 
    /// Morpheus_xx, 2011-12-13: For fallback sources no thumbnails should be used, because ALL thumbs are created as JPG. This currenly causes an issue: 
    /// Source -> no thumbnail created -> FallbackSource (PNG) -> creates a JPG thumbnail, so Alpha-Channel of FallbackSource is lost.
    /// TODO: Image class and thumbnail handling should be refactored to allow more control about image formats and thumbs usage.
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="allowThumbs">True to allow building a thumbnail of given source</param>
    /// <returns>ImageSource or null</returns>
    protected ImageSource LoadImageSource(object source, bool allowThumbs)
    {
      if (source == null)
        return null;
      bool thumbnail = allowThumbs && Thumbnail;

      _invalidateImageSourceOnResize = false;
      if (source is MediaItem)
      {
        _invalidateImageSourceOnResize = true;
        return MediaItemsHelper.CreateThumbnailImageSource((MediaItem) source, (int) Math.Max(Width, Height));
      }
      if (source is IResourceLocator)
      {
        IResourceLocator resourceLocator = (IResourceLocator) source;
        IResourceAccessor ra = resourceLocator.CreateAccessor();
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          ra.Dispose();
        else
          return new ResourceAccessorTextureImageSource(fsra, RightAngledRotation.Zero);
      }
      ImageSource result = source as ImageSource;
      if (result != null)
        return result;
      string uriSource = source as string;
      if (!string.IsNullOrEmpty(uriSource))
      {
        // Remember to adapt list of supported extensions for image player plugin...
        if (IsValidSource(uriSource))
        {
          BitmapImageSource bmi = new BitmapImageSource { UriSource = uriSource, Thumbnail = thumbnail };
          if (thumbnail)
            // Set the requested thumbnail dimension, to use the best matching format.
            bmi.ThumbnailDimension = (int) Math.Max(Width, Height);
          return bmi;
        }
        // TODO: More image types
      }
      string warnSource = source.ToString();
      if (_formerWarnURI != warnSource)
      {
        ServiceRegistration.Get<ILogger>().Warn("Image: Image source '{0}' is not supported", warnSource);
        // Remember if we already wrote a warning to the log to avoid log flooding
        _formerWarnURI = uriSource;
      }
      return null;
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

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      _sourceState.Setup = false;
      _fallbackSourceState.Setup = false;
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      ImageSourceState allocatedSource = GetLoadedSource(true);
      if (allocatedSource == null)
        base.RenderOverride(localRenderContext);
      else
      {
        // Update source geometry if necessary (source has changed, layout has changed).
        if (!allocatedSource.Setup)
        {
          allocatedSource.ImageSource.Setup(_innerRect, localRenderContext.ZOrder, SkinNeutralAR);
          allocatedSource.Setup = true;
        }
        base.RenderOverride(localRenderContext);
        allocatedSource.ImageSource.Render(localRenderContext, Stretch, StretchDirection);
      }
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

    public override void FireEvent(string eventName, RoutingStrategyEnum routingStrategy)
    {
      base.FireEvent(eventName, routingStrategy);
      if (eventName == VISIBILITY_CHANGED_EVENT)
      {
        if (!CheckVisibility())
        {
          // Element lost visibility - notify image sources
          ImageSource source = _sourceState.ImageSource;
          if (source != null)
            source.VisibilityLost();
          source = _fallbackSourceState.ImageSource;
          if (source != null)
            source.VisibilityLost();
        }
      }
    }
  }
}
