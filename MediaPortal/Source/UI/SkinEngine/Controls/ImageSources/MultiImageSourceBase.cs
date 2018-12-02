#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  public abstract class MultiImageSourceBase : TextureImageSource
  {
    protected AbstractProperty _transitionProperty;
    protected AbstractProperty _transitionInOutProperty;
    protected AbstractProperty _transitionDurationProperty;

    protected ImageContext _lastImageContext = new ImageContext();
    protected DateTime _transitionStart;
    protected bool _transitionActive = false;
    protected Random _rand = new Random();

    #region Ctor

    protected MultiImageSourceBase()
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
      MultiImageSourceBase misb = (MultiImageSourceBase)source;
      Transition = misb.Transition;
      TransitionInOut = misb.TransitionInOut;
      TransitionDuration = misb.TransitionDuration;
      FreeData();
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
      get { return (bool)_transitionInOutProperty.GetValue(); }
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

    public override Size2F SourceSize
    {
      get
      {
        Size2F currentRotatedSourceSize = _imageContext.GetRotatedSize(CurrentRawSourceSize);
        Size2F lastRotatedSourceSize = _lastImageContext.GetRotatedSize(LastRawSourceSize);
        return (_transitionActive && !lastRotatedSourceSize.IsEmpty()) ?
            MaxSizeF(lastRotatedSourceSize, currentRotatedSourceSize) : currentRotatedSourceSize;
      }
    }

    public override void Setup(RawRectangleF ownerRect, float zOrder, bool skinNeutralAR)
    {
      base.Setup(ownerRect, zOrder, skinNeutralAR);

      _lastImageContext.FrameSize = _frameSize;
    }

    public override void Render(RenderContext renderContext, Stretch stretchMode, StretchDirection stretchDirection)
    {
      Allocate();

      var currentTexture = CurrentTexture;
      Size2F currentRawSourceSize = CurrentRawSourceSize;
      RawRectangleF currentTextureClip = CurrentTextureClip;
      Vector4 frameData = new Vector4(currentRawSourceSize.Width, currentRawSourceSize.Height, (float)EffectTimer, 0);

      if (_transitionActive)
      {
        double elapsed = (SkinContext.FrameRenderingStartTime - _transitionStart).TotalSeconds / Math.Max(TransitionDuration, 0.01);
        if (elapsed > 1.0)
          _transitionActive = false;
        else
        {
          var lastTexture = LastTexture;
          Size2F lastRawSourceSize = LastRawSourceSize;
          RawRectangleF lastTextureClip = LastTextureClip;
          Vector4 lastFrameData = new Vector4(lastRawSourceSize.Width, lastRawSourceSize.Height, (float)EffectTimer, 0);

          // TODO: does null texture now work?
          var start = lastTexture;// ?? NullTexture.Texture;
          var end = currentTexture;// ?? NullTexture.Texture;

          if (start != end)
          {
            Size2F startSize = StretchSource(_lastImageContext.RotatedFrameSize, lastRawSourceSize, stretchMode, stretchDirection);
            Size2F endSize = StretchSource(_imageContext.RotatedFrameSize, currentRawSourceSize, stretchMode, stretchDirection);

            // Render transition from last texture to current texture
            _lastImageContext.Update(startSize, start, lastTextureClip);

            if (_imageContext.StartRenderTransition(renderContext, (float)elapsed, _lastImageContext,
                endSize, end, currentTextureClip, BorderColor, lastFrameData, frameData))
            {
              //_primitiveBuffer.Render(0);
              //_imageContext.EndRenderTransition();
            }
          }
          return;
        }
      }

      if (IsAllocated)
      {
        Size2F sourceSize = StretchSource(_imageContext.RotatedFrameSize, currentRawSourceSize, stretchMode, stretchDirection);
        var target = new RectangleF(
        _targetRect.Left + (_targetRect.Width() - sourceSize.Width) / 2,
        _targetRect.Top + (_targetRect.Height() - sourceSize.Height) / 2,
        sourceSize.Width, sourceSize.Height);

        if (_imageContext.StartRender(renderContext, target.Size, currentTexture, currentTextureClip, BorderColor, frameData))
        {
        }
      }
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Returns the last texture to be rendered. Must be overridden in subclasses.
    /// </summary>
    protected abstract Bitmap1 LastTexture { get; }

    /// <summary>
    /// Returns the size of the last image before any transformation but after the <see cref="LastTextureClip"/> was applied.
    /// </summary>
    protected abstract Size2F LastRawSourceSize { get; }

    /// <summary>
    /// Returns the clipping region which should be taken fron the last texture.
    /// </summary>
    protected abstract RawRectangleF LastTextureClip { get; }

    /// <summary>
    /// Returns the current texture to be rendered. Must be overridden in subclasses.
    /// </summary>
    protected abstract Bitmap1 CurrentTexture { get; }

    /// <summary>
    /// Returns the size of the current image before any transformation but after the <see cref="LastTextureClip"/> was applied.
    /// </summary>
    protected abstract Size2F CurrentRawSourceSize { get; }

    /// <summary>
    /// Returns the clipping region which should be taken fron the last texture.
    /// </summary>
    protected abstract RawRectangleF CurrentTextureClip { get; }

    /// <summary>
    /// DirectX9 does not define what happens when a NULL texture is accessed in a shader. Because of this the action
    /// of assigning a NULL texture is blocked by SharpDX. This property provides a transparent texture to use instead.
    /// </summary>
    protected TextureAsset NullTexture
    {
      get
      {
        TextureAsset nullTexture = ContentManager.Instance.GetColorTexture(BorderColor);
        if (!nullTexture.IsAllocated)
          nullTexture.Allocate();
        return nullTexture;
      }
    }

    #region Not used from base class

    protected override Bitmap1 Texture
    {
      get { return null; }
    }

    protected override Size2F RawSourceSize
    {
      get { return new Size2F(); }
    }

    protected override RectangleF TextureClip
    {
      get { return RectangleF.Empty; }
    }

    #endregion

    protected void StartTransition()
    {
      if (String.IsNullOrEmpty(Transition))
        return;
      if ((LastTexture == null || CurrentTexture == null) && !TransitionInOut)
        return;

      // Get a list of transitions to use
      string[] transitions = Transition.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
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
      base.OnEffectChanged(prop, oldValue);
      _lastImageContext.ShaderEffect = Effect;
    }

    protected override void OnHorizontalTextureAlignmentChanged(AbstractProperty property, object oldValue)
    {
      base.OnHorizontalTextureAlignmentChanged(property, oldValue);
      _lastImageContext.HorizontalTextureAlignment = HorizontalTextureAlignment;
    }

    protected override void OnVerticalTextureAlignmentChanged(AbstractProperty property, object oldValue)
    {
      base.OnVerticalTextureAlignmentChanged(property, oldValue);
      _lastImageContext.VerticalTextureAlignment = VerticalTextureAlignment;
    }

    protected override void FreeData()
    {
      base.FreeData();
      _lastImageContext.Clear();
    }

    protected Size2F MaxSizeF(Size2F a, Size2F b)
    {
      return new Size2F(Math.Max(a.Width, b.Width), Math.Max(a.Height, b.Height));
    }

    #endregion
  }
}
