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
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Common.General;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Utils;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// Acts as a source for the <see cref="Visuals.Image"/> control by wrapping a <see cref="MultiImageSource"/> or <see cref="BitmapImageSource"/>
  /// or an object that can be converted to a supported ImageSource using <see cref="ImageSourceFactory.TryCreateImageSource"/>.
  /// It enables transitions between different ImageSources and allows a delay to be specified. The image is only allocated If the attached ImageSource
  /// hasn't been changed/updated for the length of time specified in <see cref="Delay"/>. This avoids allocating lots of images when the ImageSource is updated frequently,
  /// for example when scrolling quickly through a list.
  /// </summary>
  public class ImageSourceWrapper : MultiImageSource
  {
    #region Protected Members
    protected AbstractProperty _sourceProperty;
    protected AbstractProperty _fallbackSourceProperty;
    protected AbstractProperty _delayProperty;
    protected AbstractProperty _delayInOutProperty;
    protected bool _fallbackSourceInUse = false;
    protected bool _needsUpdate = false;
    protected bool _firstAllocation = true;
    protected string _currentUri = null;
    protected string _pendingUri = null;
    protected double _pendingDelay = 0;
    protected DateTime _lastChangedTime = DateTime.MinValue;
    #endregion

    #region Ctor
    public ImageSourceWrapper()
    {
      _source = false;
      _sourceProperty = new SProperty(typeof(object), null);
      _fallbackSourceProperty = new SProperty(typeof(object), null);
      _delayProperty = new SProperty(typeof(double), 0d);
      _delayInOutProperty = new SProperty(typeof(bool), false);
      Attach();
    }
    #endregion

    #region Attach / Detach
    protected void Attach()
    {
      _sourceProperty.Attach(OnImageSourceChanged);
      _fallbackSourceProperty.Attach(OnFallbackSourceChanged);
      AttachUriProperties();
    }

    protected void Detach()
    {
      DetachUriProperties();
      _sourceProperty.Detach(OnImageSourceChanged);
      _fallbackSourceProperty.Detach(OnFallbackSourceChanged);
    }

    void AttachUriProperties()
    {
      AttachUriProperty(Source, false);
      AttachUriProperty(FallbackSource, true);
    }

    void DetachUriProperties()
    {
      DetachUriProperty(Source, false);
      DetachUriProperty(FallbackSource, true);
    }

    void AttachUriProperty(object imageSource, bool isFallback)
    {
      AbstractProperty uriProperty;
      if (!TryGetUriProperty(imageSource, out uriProperty))
        return;
      if (isFallback)
        uriProperty.Attach(OnFallbackUriChanged);
      else
        uriProperty.Attach(OnImageSourceUriChanged);
    }

    void DetachUriProperty(object imageSource, bool isFallback)
    {
      AbstractProperty uriProperty;
      if (!TryGetUriProperty(imageSource, out uriProperty))
        return;
      if (isFallback)
        uriProperty.Detach(OnFallbackUriChanged);
      else
        uriProperty.Detach(OnImageSourceUriChanged);
    }
    #endregion

    #region Public Properties
    public AbstractProperty SourceProperty { get { return _sourceProperty; } }
    /// <summary>
    /// The primary ImageSource.
    /// Can either be a path to an image or an ImageSource that has a Uri property (<see cref="MultiImageSource"/> or <see cref="BitmapImageSource"/>)
    /// or an object that can be converted to a supported ImageSource using <see cref="ImageSourceFactory.TryCreateImageSource"/>
    /// </summary>
    public object Source
    {
      get { return _sourceProperty.GetValue(); }
      set { _sourceProperty.SetValue(value); }
    }

    public AbstractProperty FallbackSourceProperty { get { return _fallbackSourceProperty; } }
    /// <summary>
    /// The ImageSource to use if the primary source cannot be loaded.
    /// Can either be a path to an image or an ImageSource that has a Uri property (<see cref="MultiImageSource"/> or <see cref="BitmapImageSource"/>)
    /// or an object that can be converted to a supported ImageSource using <see cref="ImageSourceFactory.TryCreateImageSource"/>
    /// </summary>
    public object FallbackSource
    {
      get { return _fallbackSourceProperty.GetValue(); }
      set { _fallbackSourceProperty.SetValue(value); }
    }

    public AbstractProperty DelayProperty { get { return _delayProperty; } }
    /// <summary>
    /// The amount of time in seconds to delay the transition. Useful for avoiding loading images when scrolling quickly.
    /// </summary>
    public double Delay
    {
      get { return (double)_delayProperty.GetValue(); }
      set { _delayProperty.SetValue(value); }
    }

    public AbstractProperty DelayInOutProperty { get { return _delayInOutProperty; } }
    /// <summary>
    /// Gets or sets a value indicating whether to delay the transition when either the source or target is Null.
    /// </summary>
    public bool DelayInOut
    {
      get { return (bool)_delayInOutProperty.GetValue(); }
      set { _delayInOutProperty.SetValue(value); }
    }

    public override bool IsAllocated
    {
      //Checking whether the transition is active works around a bug in MultiImageSource/Image control when transitioning to null.
      //The Image control checks whether the ImageSource is allocated before calling Render, after the transition to null is started the current texture is null
      //and IsAllocated always returns false so Render is never called again. This means the transition is never finished and the ImageSource is left in a broken state.
      get { return base.IsAllocated || TransitionActive; }
    }
    #endregion

    #region Overrides
    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ImageSourceWrapper imageSource = (ImageSourceWrapper)source;
      Source = copyManager.GetCopy(imageSource.Source);
      FallbackSource = copyManager.GetCopy(imageSource.FallbackSource);
      Delay = imageSource.Delay;
      DelayInOut = imageSource.DelayInOut;
      _needsUpdate = true;
      Attach();
    }

    public override void Allocate()
    {
      if (_firstAllocation)
      {
        //Fire UriChanged manually on the first allocation so that initially bound images are displayed
        OnImageSourceUriChanged();
        _firstAllocation = false;
      }

      //Check if it's time to switch to next texture
      CheckAndUpdateTexture();
      // Check our previous texture is allocated. Synchronous.
      TextureAsset lastTexture = _lastTexture;
      TextureAsset currentTexture = _currentTexture;
      TextureAsset nextTexture = _nextTexture;
      if (lastTexture != null && !lastTexture.IsAllocated)
        lastTexture.Allocate();
      // Check our current texture is allocated. Synchronous.
      if (currentTexture != null && !currentTexture.IsAllocated)
        currentTexture.Allocate();

      // Check our next texture is allocated. Asynchronous.
      if (nextTexture != null)
      {
        if (!_fallbackSourceInUse && nextTexture.LoadFailed)
        {
          //Load failed, try fallback source
          _fallbackSourceInUse = true;
          string uri = GetUri(FallbackSource);
          if (!string.IsNullOrEmpty(uri))
          {
            nextTexture = ContentManager.Instance.GetTexture(uri, DecodePixelWidth, DecodePixelHeight, Thumbnail);
            nextTexture.ThumbnailDimension = ThumbnailDimension;
            _nextTexture = nextTexture;
          }
        }

        //Check LoadFailed again in case we switched to fallback source
        if (!nextTexture.LoadFailed)
          nextTexture.AllocateAsync();
        else
        {
          _nextTexture = null;
          if (_currentTexture != null)
            CycleTextures(RightAngledRotation.Zero); // If new texture cannot be loaded, we allow switching to "empty" texture
          return;
        }

        if (!_transitionActive && nextTexture.IsAllocated)
          CycleTextures(RightAngledRotation.Zero);
      }
    }

    public override void Dispose()
    {
      DetachUriProperties();
      base.Dispose();
    }
    #endregion

    #region Changed Handlers
    void OnImageSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachUriProperty(oldValue, false);
      AttachUriProperty(Source, false);
      OnImageSourceUriChanged();
    }

    void OnImageSourceUriChanged(AbstractProperty property, object oldValue)
    {
      OnImageSourceUriChanged();
    }

    void OnImageSourceUriChanged()
    {
      string uri = GetUri(Source);
      if (!string.IsNullOrEmpty(uri))
        _fallbackSourceInUse = false;
      else
      {
        if (_fallbackSourceInUse)
          return;
        _fallbackSourceInUse = true;
        uri = GetUri(FallbackSource);
      }
      ScheduleUpdate(uri);
    }

    void OnFallbackSourceChanged(AbstractProperty property, object oldValue)
    {
      DetachUriProperty(oldValue, true);
      AttachUriProperty(FallbackSource, true);
      OnFallbackUriChanged();
    }

    void OnFallbackUriChanged(AbstractProperty property, object oldValue)
    {
      OnFallbackUriChanged();
    }

    void OnFallbackUriChanged()
    {
      if (!_fallbackSourceInUse)
        return;
      string uri = GetUri(FallbackSource);
      ScheduleUpdate(uri);
    }
    #endregion

    #region Image Updating
    string GetUri(object imageSource)
    {
      if (imageSource == null)
        return null;

      string uri = imageSource as string;
      if (uri != null)
        return uri;

      ImageSource convertedSource;
      if (!ImageSourceFactory.TryCreateImageSource(imageSource, 0, 0, out convertedSource))
        return null;

      AbstractProperty uriProperty;
      if (TryGetUriProperty(convertedSource, out uriProperty))
        return uriProperty.GetValue() as string;

      ServiceRegistration.Get<ILogger>().Warn("ImageSourceWrapper: Unsupported ImageSource type '{0}'", imageSource.GetType());
      return null;
    }

    void ScheduleUpdate(string uri)
    {
      bool force = _firstAllocation || (!DelayInOut && (string.IsNullOrEmpty(_pendingUri) || string.IsNullOrEmpty(uri)));
      _pendingDelay = force ? 0 : Delay;
      _pendingUri = uri;
      _lastChangedTime = DateTime.Now;
      _needsUpdate = true;
    }

    void CheckAndUpdateTexture()
    {
      if (!_needsUpdate || (SkinContext.FrameRenderingStartTime - _lastChangedTime).TotalSeconds < _pendingDelay)
        return;

      _needsUpdate = false;
      string uri = _pendingUri;
      if (string.IsNullOrEmpty(uri))
      {
        _nextTexture = null;
        if (_currentTexture != null)
          CycleTextures(RightAngledRotation.Zero);
      }
      else
      {
        _currentUri = uri;
        _nextTexture = ContentManager.Instance.GetTexture(uri, DecodePixelWidth, DecodePixelHeight, Thumbnail);
        _nextTexture.ThumbnailDimension = ThumbnailDimension;
      }
    }

    bool TryGetUriProperty(object imageSource, out AbstractProperty uriProperty)
    {
      uriProperty = null;
      if (imageSource == null)
        return false;

      MultiImageSource miSource = imageSource as MultiImageSource;
      if (miSource != null)
      {
        uriProperty = miSource.UriSourceProperty;
        return true;
      }

      BitmapImageSource bSource = imageSource as BitmapImageSource;
      if (bSource != null)
      {
        uriProperty = bSource.UriSourceProperty;
        return true;
      }
      return false;
    }
    #endregion
  }
}