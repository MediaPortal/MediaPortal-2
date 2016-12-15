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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// Image source that has a <see cref="Texture"/> as image data.
  /// </summary>
  /// <remarks>
  /// This class does the rendering of a texture given by the property <see cref="Texture"/> which must be overridden by sub classes.
  /// Sub classes can provide an arbitrary texture to render.
  /// </remarks>
  public abstract class TextureImageSource : ImageSource
  {
    #region Protected fields

    protected AbstractProperty _borderColorProperty;
    protected AbstractProperty _effectProperty;
    protected AbstractProperty _effectTimerProperty;
    protected AbstractProperty _horizontalTextureAlignmentProperty;
    protected AbstractProperty _verticalTextureAlignmentProperty;

    protected PrimitiveBuffer _primitiveBuffer;
    protected ImageContext _imageContext = new ImageContext();
    protected SizeF _frameSize;

    #endregion

    #region Ctor

    protected TextureImageSource()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Deallocate();
    }

    void Init()
    {
      _borderColorProperty = new SProperty(typeof(Color), ColorConverter.FromArgb(0, Color.Black));
      _effectProperty = new SProperty(typeof(string), null);
      _effectTimerProperty = new SProperty(typeof(double), 0.0);
      _horizontalTextureAlignmentProperty = new SProperty(typeof(HorizontalTextureAlignmentEnum), HorizontalTextureAlignmentEnum.Center);
      _verticalTextureAlignmentProperty = new SProperty(typeof(VerticalTextureAlignmentEnum), VerticalTextureAlignmentEnum.Center);
    }

    void Attach()
    {
      _effectProperty.Attach(OnEffectChanged);
      _horizontalTextureAlignmentProperty.Attach(OnHorizontalTextureAlignmentChanged);
      _verticalTextureAlignmentProperty.Attach(OnVerticalTextureAlignmentChanged);
    }

    void Detach()
    {
      _effectProperty.Detach(OnEffectChanged);
      _horizontalTextureAlignmentProperty.Detach(OnHorizontalTextureAlignmentChanged);
      _verticalTextureAlignmentProperty.Detach(OnVerticalTextureAlignmentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Detach();
      TextureImageSource tis= (TextureImageSource) source;
      BorderColor = tis.BorderColor;
      Effect = tis.Effect;
      EffectTimer = tis.EffectTimer;
      HorizontalTextureAlignment = tis.HorizontalTextureAlignment;
      VerticalTextureAlignment = tis.VerticalTextureAlignment;
      
      Attach();
      FreeData();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the color of the border around images to small for the frame.
    /// </summary>
    public Color BorderColor
    {
      get { return (Color) _borderColorProperty.GetValue(); }
      set { _borderColorProperty.SetValue(value); }
    }

    public AbstractProperty BorderColorProperty
    {
      get { return _borderColorProperty; }
    }

    /// <summary>
    /// Gets or sets the <see cref="ImageContext"/> effect to apply to the image.
    /// </summary>
    public string Effect
    {
      get { return (string) _effectProperty.GetValue(); }
      set { _effectProperty.SetValue(value); }
    }

    public AbstractProperty EffectProperty
    {
      get { return _effectProperty; }
    }

    /// <summary>
    /// Gets or sets the time value that will be passed to shader effects. This animation using storyboards.
    /// </summary>
    public double EffectTimer
    {
      get { return (double) _effectTimerProperty.GetValue(); }
      set { _effectTimerProperty.SetValue(value); }
    }

    public AbstractProperty EffectTimerProperty
    {
      get { return _effectTimerProperty; }
    }

    /// <summary>
    /// Gets or sets the horizonatal alignment of the texture within the target rectangle.
    /// </summary>
    public HorizontalTextureAlignmentEnum HorizontalTextureAlignment
    {
      get { return (HorizontalTextureAlignmentEnum)_horizontalTextureAlignmentProperty.GetValue(); }
      set { _horizontalTextureAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty HorizontalTextureAlignmentProperty
    {
      get { return _horizontalTextureAlignmentProperty; }
    }

    /// <summary>
    /// Gets or sets the vertical alignment of the texture within the target rectangle.
    /// </summary>
    public VerticalTextureAlignmentEnum VerticalTextureAlignment
    {
      get { return (VerticalTextureAlignmentEnum)_verticalTextureAlignmentProperty.GetValue(); }
      set { _verticalTextureAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalTextureAlignmentProperty
    {
      get { return _verticalTextureAlignmentProperty; }
    }

    #endregion

    #region ImageSource implementation

    public override SizeF SourceSize
    {
      get { return _imageContext.GetRotatedSize(RawSourceSize); }
    }

    public override void Deallocate()
    {
      PrimitiveBuffer.DisposePrimitiveBuffer(ref _primitiveBuffer);
      FreeData();
    }

    public override void Setup(RectangleF ownerRect, float zOrder, bool skinNeutralAR)
    {
      PositionColoredTextured[] verts = new PositionColoredTextured[4];

      // Upper left
      verts[0].X = ownerRect.Left;
      verts[0].Y = ownerRect.Top;
      verts[0].Color = 0;
      verts[0].Tu1 = 0.0f;
      verts[0].Tv1 = 0.0f;
      verts[0].Z = zOrder;

      // Bottom left
      verts[1].X = ownerRect.Left;
      verts[1].Y = ownerRect.Bottom;
      verts[1].Color = 0;
      verts[1].Tu1 = 0.0f;
      verts[1].Tv1 = 1.0f;
      verts[1].Z = zOrder;

      // Bottom right
      verts[2].X = ownerRect.Right;
      verts[2].Y = ownerRect.Bottom;
      verts[2].Color = 0;
      verts[2].Tu1 = 1.0f;
      verts[2].Tv1 = 1.0f;
      verts[2].Z = zOrder;

      // Upper right
      verts[3].X = ownerRect.Right;
      verts[3].Y = ownerRect.Top;
      verts[3].Color = 0;
      verts[3].Tu1 = 1.0f;
      verts[3].Tv1 = 0.0f;
      verts[3].Z = zOrder;

      PrimitiveBuffer.SetPrimitiveBuffer(ref _primitiveBuffer, ref verts, PrimitiveType.TriangleFan);

      _frameSize = skinNeutralAR ? ImageContext.AdjustForSkinAR(ownerRect.Size) : ownerRect.Size;
      _imageContext.FrameSize = _frameSize;
    }

    public override void Render(RenderContext renderContext, Stretch stretchMode, StretchDirection stretchDirection)
    {
      if (!IsAllocated)
        return;
      SizeF rawSourceSize = RawSourceSize;
      SizeF modifiedSourceSize = StretchSource(_imageContext.RotatedFrameSize, rawSourceSize, stretchMode, stretchDirection);
      Vector4 frameData = new Vector4(rawSourceSize.Width, rawSourceSize.Height, (float) EffectTimer, 0);
      if (_primitiveBuffer != null && _imageContext.StartRender(renderContext, modifiedSourceSize, Texture, TextureClip, BorderColor, frameData))
      {
        _primitiveBuffer.Render(0);
        _imageContext.EndRender();
      }
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Returns the texture to be rendered. Must be overridden in subclasses.
    /// </summary>
    protected abstract Texture Texture { get; }

    /// <summary>
    /// Returns the size of the last image before any transformation but after the <see cref="TextureClip"/> was applied.
    /// </summary>
    protected abstract SizeF RawSourceSize { get; }

    /// <summary>
    /// Returns the clipping region which should be taken fron the last texture.
    /// </summary>
    protected abstract RectangleF TextureClip { get; }

    protected virtual void OnEffectChanged(AbstractProperty prop, object oldValue)
    {
      _imageContext.ShaderEffect = Effect;
    }

    protected virtual void OnHorizontalTextureAlignmentChanged(AbstractProperty property, object oldValue)
    {
      _imageContext.HorizontalTextureAlignment = HorizontalTextureAlignment;
    }

    protected virtual void OnVerticalTextureAlignmentChanged(AbstractProperty property, object oldValue)
    {
      _imageContext.VerticalTextureAlignment = VerticalTextureAlignment;
    }

    protected virtual void FreeData()
    {
      _imageContext.Clear();
    }

    #endregion
  }
}
