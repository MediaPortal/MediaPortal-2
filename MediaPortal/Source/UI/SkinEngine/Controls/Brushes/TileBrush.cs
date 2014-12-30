#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.Controls.Brushes.Animation;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using Stretch = MediaPortal.UI.SkinEngine.Controls.Visuals.Stretch;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public enum TileMode
  {
    // No tiling
    None,
    // Content is tiled
    Tile,
    // Content is tiled and flipped around x-axis
    FlipX,
    // Content is tiled and flipped around y-axis
    FlipY,
    // Content is tiled and flipped around both x and y-axis
    FlipXY
  };

  public abstract class TileBrush : Brush, IRenderBrush
  {
    #region Consts

    protected readonly RectangleF CROP_FULLSIZE = new RectangleF(0, 0, 1, 1);

    #endregion

    #region Protected fields

    protected AbstractProperty _alignmentXProperty;
    protected AbstractProperty _alignmentYProperty;
    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _viewPortProperty;
    protected AbstractProperty _tileModeProperty;
    protected AbstractProperty _animationProperty;
    protected AbstractProperty _animationEnabledProperty;

    protected bool _refresh = true;
    protected Vector4 _textureViewport;
    protected Matrix _relativeTransformCache;

    protected IBitmapAsset2D _tex;
    protected RectangleF _textureClip;

    #endregion

    #region Ctor

    protected TileBrush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      TryDispose(ref _tex);
    }

    void Init()
    {
      _alignmentXProperty = new SProperty(typeof(AlignmentX), AlignmentX.Center);
      _alignmentYProperty = new SProperty(typeof(AlignmentY), AlignmentY.Center);
      _stretchProperty = new SProperty(typeof(Stretch), Stretch.Fill);
      _tileModeProperty = new SProperty(typeof(TileMode), TileMode.None);
      _viewPortProperty = new SProperty(typeof(Vector4), new Vector4(0, 0, 1, 1));
      _animationProperty = new SProperty(typeof(IImageAnimator), null);
      _animationEnabledProperty = new SProperty(typeof(bool), false);
    }

    void Attach()
    {
      _alignmentXProperty.Attach(OnPropertyChanged);
      _alignmentYProperty.Attach(OnPropertyChanged);
      _stretchProperty.Attach(OnPropertyChanged);
      _tileModeProperty.Attach(OnPropertyChanged);
      _viewPortProperty.Attach(OnPropertyChanged);
      _animationProperty.Attach(OnAnimationChanged);
      _animationEnabledProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _alignmentXProperty.Detach(OnPropertyChanged);
      _alignmentYProperty.Detach(OnPropertyChanged);
      _stretchProperty.Detach(OnPropertyChanged);
      _tileModeProperty.Detach(OnPropertyChanged);
      _viewPortProperty.Detach(OnPropertyChanged);
      _animationProperty.Detach(OnAnimationChanged);
      _animationEnabledProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      TileBrush b = (TileBrush)source;
      AlignmentX = b.AlignmentX;
      AlignmentY = b.AlignmentY;
      Stretch = b.Stretch;
      Tile = b.Tile;
      ViewPort = copyManager.GetCopy(b.ViewPort);
      Animation = copyManager.GetCopy(b.Animation);
      AnimationEnabled = b.AnimationEnabled;
      _refresh = true;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty AlignmentXProperty
    {
      get { return _alignmentXProperty; }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of the image within the viewport.
    /// </summary>
    public AlignmentX AlignmentX
    {
      get { return (AlignmentX)_alignmentXProperty.GetValue(); }
      set { _alignmentXProperty.SetValue(value); }
    }

    public AbstractProperty AlignmentYProperty
    {
      get { return _alignmentYProperty; }
    }

    /// <summary>
    /// Gets or sets the vertical alignment of the image within the viewport.
    /// </summary>
    public AlignmentY AlignmentY
    {
      get { return (AlignmentY)_alignmentYProperty.GetValue(); }
      set { _alignmentYProperty.SetValue(value); }
    }

    public AbstractProperty StretchProperty
    {
      get { return _stretchProperty; }
    }

    /// <summary>
    /// Gets or sets the <see cref="Stretch"/> scaling used to fit the image within the viewport.
    /// </summary>
    public Stretch Stretch
    {
      get { return (Stretch)_stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public AbstractProperty ViewPortProperty
    {
      get { return _viewPortProperty; }
    }

    /// <summary>
    /// Gets or sets the viewport sub-rect that the image is displayed in. Coordinates are in the 0-1 range.
    /// </summary>
    public Vector4 ViewPort
    {
      get { return (Vector4)_viewPortProperty.GetValue(); }
      set { _viewPortProperty.SetValue(value); }
    }

    public AbstractProperty TileProperty
    {
      get { return _tileModeProperty; }
    }

    /// <summary>
    /// Gets or sets the <see cref="TileMode"/> used when the <see cref="ViewPort"/> is smaller than the drawing target.
    /// </summary>
    public TileMode Tile
    {
      get { return (TileMode)_tileModeProperty.GetValue(); }
      set { _tileModeProperty.SetValue(value); }
    }

    public AbstractProperty AnimationProperty
    {
      get { return _animationProperty; }
    }

    public IImageAnimator Animation
    {
      get { return (IImageAnimator)_animationProperty.GetValue(); }
      set { _animationProperty.SetValue(value); }
    }

    public AbstractProperty AnimationEnabledProperty
    {
      get { return _animationEnabledProperty; }
    }

    public bool AnimationEnabled
    {
      get { return (bool)_animationEnabledProperty.GetValue(); }
      set { _animationEnabledProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Gets the actual limits of the image within it's texture.
    /// </summary>
    protected virtual Vector2 TextureMaxUV
    {
      get { return new Vector2(1.0f, 1.0f); }
    }

    protected override void OnRelativeTransformChanged(IObservable trans)
    {
      _refresh = true;
      base.OnRelativeTransformChanged(trans);
    }

    protected override void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      _refresh = true;
      base.OnPropertyChanged(prop, oldValue);
    }

    private void OnAnimationChanged(AbstractProperty property, object oldvalue)
    {
      Reset();
    }

    protected bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      //if (Texture == null)
      //  return false;

      //Matrix finalTransform = renderContext.Transform.Clone();

      //if (_refresh)
      //{
      //  RefreshEffectParameters();
      //  _effect = ContentManager.Instance.GetEffect(_simplemode ? EFFECT_TILE_SIMPLE : EFFECT_TILE);
      //  _refresh = false;
      //}

      //if (_simplemode)
      //  SetSimpleEffectParameters(renderContext);
      //else
      //  SetEffectParameters(renderContext);

      //_effect.StartRender(Texture, finalTransform);

      return true;
    }

    public void EndRender()
    {
      //if (Texture == null)
      //  return;
      //if (_effect != null)
      //  _effect.EndRender();
    }

    public virtual bool RenderContent(RenderContext renderContext)
    {
      if (_refresh)
      {
        RefreshEffectParameters();
        _refresh = false;
      }
      SetEffectParameters(renderContext);
      return true;
    }

    protected void RefreshEffectParameters()
    {
      float w = _vertsBounds.Width;
      float h = _vertsBounds.Height;
      Vector2 maxuv = TextureMaxUV;

      Vector4 brushRect = ViewPort;
      // Determine image rect in viewport space
      if (Stretch != Stretch.Fill)
      {
        // Convert brush dimensions to viewport space
        Vector2 brushSize = BrushDimensions;
        brushSize.X /= w;
        brushSize.Y /= h;
        switch (Stretch)
        {
          case Stretch.None:
            // Center (or alignment), original size
            break;

          case Stretch.Uniform:
            // Center (or alignment), keep aspect ratio and show borders
            {
              float ratio = Math.Min(ViewPort.Z / brushSize.X, ViewPort.W / brushSize.Y);
              brushSize.X *= ratio;
              brushSize.Y *= ratio;
            }
            break;

          case Stretch.UniformToFill:
            // Center (or alignment), keep aspect ratio, zoom in to avoid borders
            {
              float ratio = Math.Max(ViewPort.Z / brushSize.X, ViewPort.W / brushSize.Y);
              brushSize.X *= ratio;
              brushSize.Y *= ratio;
            }
            break;
        }
        // Align brush in viewport
        brushRect = AlignBrushInViewport(brushSize);
      }
      // Compensate for any texture borders
      brushRect.Z /= maxuv.X;
      brushRect.W /= maxuv.Y;

      float repeatx = 1.0f / brushRect.Z;
      float repeaty = 1.0f / brushRect.W;

      // Transform ViewPort into Texture-space and store for later use in tiling
      _textureViewport = new Vector4
        {
          X = ViewPort.X * repeatx - brushRect.X * repeatx,
          Y = ViewPort.Y * repeaty - brushRect.Y * repeaty,
          Z = ViewPort.Z * repeatx,
          W = ViewPort.W * repeaty
        };

      // This structure is used for modifying vertex texture coords to position the brush texture
      //_brushTransform = new Vector4(brushRect.X * repeatx, brushRect.Y * repeaty, repeatx, repeaty);

      _relativeTransformCache = RelativeTransform == null ? Matrix.Identity : Matrix.Invert(RelativeTransform.GetTransform());
    }

    protected void SetEffectParameters(RenderContext renderContext)
    {
      Vector2 uvoffset = new Vector2(0.0f, 0.0f);
      BitmapBrush bmpBrush = _brush2D as BitmapBrush;
      if (bmpBrush == null)
        return;
      switch (Tile)
      {
        case TileMode.Tile:
          // Tile both directions
          bmpBrush.ExtendModeX = ExtendMode.Wrap;
          bmpBrush.ExtendModeY = ExtendMode.Wrap;
          break;
        case TileMode.FlipX:
          // Tile both directions but mirror texture on alterate repeats in u/x direction
          bmpBrush.ExtendModeX = ExtendMode.Mirror;
          bmpBrush.ExtendModeY = ExtendMode.Wrap;
          uvoffset.X = 1.0f - TextureMaxUV.X;
          break;
        case TileMode.FlipY:
          // Tile both directions but mirror texture on alterate repeats in v/y direction
          bmpBrush.ExtendModeX = ExtendMode.Wrap;
          bmpBrush.ExtendModeY = ExtendMode.Mirror;
          uvoffset.Y = 1.0f - TextureMaxUV.Y;
          break;
        case TileMode.FlipXY:
          // Tile and mirror texture in both directions
          bmpBrush.ExtendModeX = ExtendMode.Mirror;
          bmpBrush.ExtendModeY = ExtendMode.Mirror;
          uvoffset = TextureMaxUV;
          uvoffset.X = 1.0f - uvoffset.X;
          uvoffset.Y = 1.0f - uvoffset.Y;
          break;
        case TileMode.None:
        default:
          // No tiling
          bmpBrush.ExtendModeX = ExtendMode.Clamp;
          bmpBrush.ExtendModeY = ExtendMode.Clamp;
          //_effect.Parameters[PARAM_TILE_U] = 4; // D3DTADDRESS_BORDER
          //_effect.Parameters[PARAM_TILE_V] = 4; // D3DTADDRESS_BORDER
          break;
      }
      _textureClip = GetTextureClip();
      SetBrushTransform();
    }

    public void Reset()
    {
      IImageAnimator animator = Animation;
      if (animator != null)
        animator.Initialize();
      _refresh = true;
    }

    public RectangleF GetTextureClip()
    {
      IImageAnimator animator = Animation;
      // TODO: Execute animation in own timer
      if (animator == null || !AnimationEnabled)
        return CROP_FULLSIZE;

      Size size = new Size((int)BrushDimensions.X, (int)BrushDimensions.Y);
      Size outputSize = new Size((int)_vertsBounds.Width, (int)_vertsBounds.Height);
      RectangleF textureClip = animator.GetZoomRect(size, outputSize, DateTime.Now);

      return textureClip;
    }

    // Transform brush into control scope
    protected override void SetBrushTransform()
    {
      if (_brush2D == null || _vertsBounds.IsEmpty || _tex == null)
        return;
      Matrix3x2 transform = Matrix3x2.Identity;
      float contentWidth = _tex.Width;
      float contentHeight = _tex.Height;
      float brushOffsetX = 0f;
      float brushOffsetY = 0f;
      if (!_textureClip.IsEmpty)
      {
        contentWidth = _textureClip.Width * _tex.Width;
        contentHeight = _textureClip.Height * _tex.Height;
        brushOffsetX = _textureClip.Left * _tex.Width;
        brushOffsetY = _textureClip.Top * _tex.Height;
      }
      transform *= Matrix3x2.Scaling(_vertsBounds.Width / contentWidth, _vertsBounds.Height / contentHeight);
      transform *= Matrix3x2.Translation(_vertsBounds.X - brushOffsetX, _vertsBounds.Y - brushOffsetY);
      _brush2D.Transform = transform;
    }

    protected virtual Vector4 AlignBrushInViewport(Vector2 brushSize)
    {
      Vector4 rect = new Vector4();
      switch (AlignmentX)
      {
        case AlignmentX.Left:
          rect.X = ViewPort.X;
          break;

        case AlignmentX.Right:
          rect.X = ViewPort.X + ViewPort.Z - brushSize.X;
          break;

        case AlignmentX.Center:
        default:
          rect.X = ViewPort.X + (ViewPort.Z - brushSize.X) / 2;
          break;
      }
      switch (AlignmentY)
      {
        case AlignmentY.Top:
          rect.Y = ViewPort.Y;
          break;

        case AlignmentY.Bottom:
          rect.Y = ViewPort.Y + ViewPort.W - brushSize.Y;
          break;

        case AlignmentY.Center:
        default:
          rect.Y = ViewPort.Y + (ViewPort.W - brushSize.Y) / 2;
          break;
      }
      rect.Z = brushSize.X;
      rect.W = brushSize.Y;
      return rect;
    }

    protected virtual Vector2 BrushDimensions
    {
      get { return new Vector2(1, 1); }
    }
  }
}
