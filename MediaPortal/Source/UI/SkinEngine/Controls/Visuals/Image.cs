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
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

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
    protected SizeF _lastImageSourceSize = new SizeF();
    protected string _formerWarnURI = null;

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
    }

    void Detach()
    {
      _sourceProperty.Detach(OnSourceChanged);
      _fallbackSourceProperty.Detach(OnFallbackSourceChanged);
      _stretchProperty.Detach(OnArrangeGetsInvalid);
      _stretchDirectionProperty.Detach(OnArrangeGetsInvalid);
      _thumbnailProperty.Detach(OnArrangeGetsInvalid);
      _skinNeutralProperty.Detach(OnArrangeGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Image i = (Image)source;
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
      get { return (Stretch)_stretchProperty.GetValue(); }
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
      get { return (StretchDirection)_stretchDirectionProperty.GetValue(); }
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
      get { return (bool)_thumbnailProperty.GetValue(); }
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
      get { return (bool)_skinNeutralProperty.GetValue(); }
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
      get { return (bool)_hasImageProperty.GetValue(); }
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

      ImageSource imageSource = _sourceState.ImageSource;
      // Find a new image source
      if (imageSource == null)
      {
        imageSource = _sourceState.ImageSource = LoadImageSource(Source, true);
        _sourceState.Setup = false;
      }

      if (imageSource != null && !imageSource.IsAllocated)
      {
        _sourceState.Setup = false;
        imageSource.Allocate();
      }

      if (imageSource != null && imageSource.IsAllocated)
        allocatedSource = _sourceState;
      else
      {
        ImageSource fallbackSource = _fallbackSourceState.ImageSource;
        // If the source image could not load yet, try fallback image
        if (fallbackSource == null)
        {
          fallbackSource = _fallbackSourceState.ImageSource = LoadImageSource(FallbackSource, false);
          _fallbackSourceState.Setup = false;
        }

        if (fallbackSource != null && !fallbackSource.IsAllocated)
        {
          _fallbackSourceState.Setup = false;
          fallbackSource.Allocate();
        }

        if (fallbackSource != null && fallbackSource.IsAllocated)
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
      // Web URIs often doesn't contain a image based extensions. For absolute uri we expect them to point to a valid image source.
      // TODO: the list of image extensions should be extensible, i.e. the FreeImage library support much more image types to be loaded!
      Uri uri;
      if (Uri.TryCreate(uriSource, UriKind.Absolute, out uri) || lower.EndsWith(".png") || lower.EndsWith(".bmp") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".gif"))
        return true;

      if (Thumbnail && (lower.EndsWith(".avi") || lower.EndsWith(".ts") || lower.EndsWith(".mkv")))
        return true;

      return false;
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

      ImageSource imageSource;
      if (ImageSourceFactory.TryCreateImageSource(source, (int)Width, (int)Height, out imageSource))
      {
        return imageSource;
      }

      string uriSource = source as string;
      if (!string.IsNullOrEmpty(uriSource))
      {
        // Remember to adapt list of supported extensions for image player plugin...
        if (IsValidSource(uriSource))
        {
          BitmapImageSource bmi = new BitmapImageSource { UriSource = uriSource, Thumbnail = thumbnail };
          if (thumbnail)
            // Set the requested thumbnail dimension, to use the best matching format.
            // Note: Math.Max returns NaN if one argument is NaN (which casts to int.MinValue), so the additional Max with 0 catches this
            bmi.ThumbnailDimension = Math.Max((int)Math.Max(Width, Height), 0);
          return bmi;
        }
        // TODO: More image types
      }
      string warnSource = source.ToString();
      if (_formerWarnURI != warnSource)
      {
        if (!string.IsNullOrEmpty(warnSource))
          ServiceRegistration.Get<ILogger>().Warn("Image: Image source '{0}' is not supported", warnSource);

        // Remember if we already wrote a warning to the log to avoid log flooding
        _formerWarnURI = warnSource;
      }
      return null;
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      ImageSourceState allocatedSource = GetLoadedSource(false);
      if (allocatedSource == null)
      {
        _lastImageSourceSize = new SizeF();
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

    protected override void DoFireEvent(string eventName)
    {
      base.DoFireEvent(eventName);
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
