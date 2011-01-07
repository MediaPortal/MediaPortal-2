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
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// Like <see cref="BitmapImage"/>, <see cref="MultiImage"/> acts as a source for the <see cref="Visuals.Image"/> control
  /// to access to convential image formats. The primary difference between these two classes is that
  /// <see cref="MultiImage"/> is optimised for asyncronous image loading and frequent image changes,
  /// such as in a slide-show, and allows animated transitions between images.
  /// </summary>
  class MultiImage : BitmapImage
  {
    protected AbstractProperty _transitionProperty;
    protected AbstractProperty _transitionInOutProperty;
    protected AbstractProperty _transitionDurationProperty;

    protected TextureAsset _lastTexture = null;
    protected TextureAsset _nextTexture = null;
    protected ImageContext _lastImageContext = new ImageContext();
    protected DateTime _transitionStart;
    protected bool _transitionActive = false;
    protected Random _rand = new Random();
    protected bool _uriChanged = true;
    protected Vector4 _lastFrameData;

    #region Ctor

    public MultiImage()
    {
      Init();
    }

    void Init()
    {
      _transitionProperty = new SProperty(typeof(string), null);
      _transitionDurationProperty = new SProperty(typeof(double), 2.0);
      _transitionInOutProperty = new SProperty(typeof(bool), true);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      MultiImage b = (MultiImage) source;
      Transition = b.Transition;
      TransitionDuration = b.TransitionDuration;
      TransitionInOut = b.TransitionInOut;
      FreeTextures();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the transition(s) to use when changing images. A ';' deliminated list may also be set, in which
    /// case a transition will be chosen randomly from that list.
    /// </summary>
    public string Transition
    {
      get { return (string)_transitionProperty.GetValue(); }
      set { _transitionProperty.SetValue(value); }
    }

    public AbstractProperty TransitionProperty
    {
      get { return _transitionProperty; }
    }

    /// <summary>
    /// Gets or sets the a value indicating whether transitions should be used when either the source or target is Null.
    /// </summary>
    public bool TransitionInOut
    {
      get { return (bool) _transitionInOutProperty.GetValue(); }
      set { _transitionInOutProperty.SetValue(value); }
    }

    public AbstractProperty TransitionInOutProperty
    {
      get { return _transitionInOutProperty; }
    }

    /// <summary>
    /// Gets or sets the duration of transitions in seconds.
    /// </summary>
    public double TransitionDuration
    {
      get { return (double)_transitionDurationProperty.GetValue(); }
      set { _transitionDurationProperty.SetValue(value); }
    }

    public AbstractProperty TransitionDurationProperty
    {
      get { return _transitionDurationProperty; }
    }

    /// <summary>
    /// Gets a value indicating whether a transition is currently running.
    /// </summary>
    /// <returns></returns>
    public bool TransitionActive
    {
      get { return _transitionActive; }
    }

    #endregion

    #region ImageSource implementation

    public override void Allocate()
    {
      if (_uriChanged)
      {
        string uri = UriSource;
        if (String.IsNullOrEmpty(uri))
        {
          _nextTexture = null;
          if (_texture != null)
            CycleTextures();
        }
        else
          _nextTexture = ServiceRegistration.Get<ContentManager>().GetTexture(uri, DecodePixelWidth, DecodePixelHeight, Thumbnail);
        _uriChanged = false;
      }
      // Check our previous texture is allocated. Syncronous.
      if (_lastTexture != null && !_lastTexture.IsAllocated)
        _lastTexture.Allocate();
      // Check our current texture is allocated. Syncronous.
      if (_texture != null && !_texture.IsAllocated)
        _texture.Allocate();
      // Check our next texture is allocated. Asyncronous.
      if (_nextTexture != null)
      {
        if (!_nextTexture.LoadFailed)
          _nextTexture.AllocateAsync();
        if (!_transitionActive && _nextTexture.IsAllocated)
          CycleTextures();
      }
    }

    public override void Setup(RectangleF ownerRect, float zOrder, bool skinNeutralAR)
    {
      base.Setup(ownerRect, zOrder, skinNeutralAR);

      _lastImageContext.FrameSize = _imageContext.FrameSize;
    }

    public override void Render(RenderContext renderContext, Stretch stretchMode, StretchDirection stretchDirection)
    {
      Allocate();

      _frameData.Z = (float)EffectTimer;

      if (_transitionActive)
      {
        double elapsed = (SkinContext.FrameRenderingStartTime - _transitionStart).TotalSeconds / Math.Max(TransitionDuration, 0.01);
        if (elapsed > 1.0)
          _transitionActive = false;
        else 
        {
          TextureAsset start = _lastTexture ?? NullTexture;
          TextureAsset end = _texture ?? NullTexture;

          if (start != end)
          {
            SizeF startSize = StretchSource(_lastImageContext.FrameSize, new SizeF(start.Width, start.Height), stretchMode, stretchDirection);
            SizeF endSize = StretchSource(_imageContext.FrameSize, new SizeF(end.Width, end.Height), stretchMode, stretchDirection);

            // Render transition from image A (previous) to image B (current/next)
            _lastImageContext.Update(startSize, start);
            _imageContext.Update(endSize, end);

            _lastFrameData.Z = _frameData.Z;
            if (_imageContext.StartRenderTransition(renderContext, (float)elapsed, _lastImageContext, BorderColor.ToArgb(), _lastFrameData, _frameData))
            {
              _primitiveBuffer.Render(0);
              _imageContext.EndRenderTransition();
            }
          }
          return;
        }
      }

      if (IsAllocated)
      {
        SizeF sourceSize = StretchSource(_imageContext.FrameSize, new SizeF(_texture.Width, _texture.Height), stretchMode, stretchDirection);
        if (_imageContext.StartRender(renderContext, sourceSize, _texture, BorderColor.ToArgb(), _frameData))
        {
          _primitiveBuffer.Render(0);
          _imageContext.EndRender();
        }
      }
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// DirectX9 does not define what happens when a NULL texture is accessed in a shader. Because of this the action
    /// of assigning a NULL texture is blocked by SlimDX. This property provides a transparent texture to use instead.
    /// </summary>
    protected TextureAsset NullTexture
    {
      get
      {
        TextureAsset nullTexture = ServiceRegistration.Get<ContentManager>().GetColorTexture(BorderColor);
        if (!nullTexture.IsAllocated)
          nullTexture.Allocate();
        return nullTexture;
      }
    }

    protected void CycleTextures()
    {
      // Current -> Last
      _lastTexture = _texture;
      _lastImageContext = _imageContext;
      _lastFrameData = _frameData;
      // Next -> Current
      _texture = _nextTexture;
      _imageContext = new ImageContext
        {
            FrameSize = _lastImageContext.FrameSize,
            ShaderEffect = _lastImageContext.ShaderEffect
        };
      _frameData = new Vector4(_texture.Width, _texture.Height, 0.0f, 0.0f);
      // Clear next
      _nextTexture = null;

      if (_lastTexture != _texture)
      {
        StartTransition();
        FireChanged();
      }
    }

    protected void StartTransition()
    {
      if (String.IsNullOrEmpty(Transition))
        return;
      if ((_lastTexture == null || _texture == null) && !TransitionInOut)
        return;

      // Get a list of transitions to use
      string[] transitions = Transition.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
      if (transitions.Length == 0)
        return;
      // Choose a random transition
      int selected = _rand.Next(transitions.Length);
      int count = 0;

      // If for some reason this transition doesn't exist continue trying
      int transitionSearchLimit = Math.Min(10, transitions.Length);
      while (!TransitionExists(transitions[selected]) && count++ < transitionSearchLimit)
        selected = _rand.Next(transitions.Length);

      // Too many failures, abort transition 
      if (count == transitionSearchLimit)
        return;

      // Now that we have a valid transition we can start the action...
      _transitionStart = SkinContext.FrameRenderingStartTime;
      _imageContext.ShaderTransition = transitions[selected];
      _transitionActive = true;
    }

    protected bool TransitionExists(string resourceName)
    {
      return SkinContext.SkinResources.GetResourceFilePath(string.Format(@"{0}\{1}.fx", SkinResources.SHADERS_DIRECTORY, resourceName)) != null;
    }

    protected override void OnEffectChanged(AbstractProperty prop, object oldValue)
    {
      _imageContext.ShaderEffect = Effect;
      _lastImageContext.ShaderEffect = Effect;
    }

    protected override void OnSourceChanged(AbstractProperty prop, object oldValue)
    {
      _uriChanged = true;
    }

    protected override void FreeTextures()
    {
      base.FreeTextures();
      _lastTexture = null;
      _nextTexture = null;
      _lastImageContext.Clear();
    }

    #endregion
  }
}
