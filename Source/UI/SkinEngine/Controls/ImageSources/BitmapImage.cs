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
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// <see cref="BitmapImage"/> acts as a source provider / renderer for the <see cref="Image"/> control and most conventional image formats are supportted. 
  /// All images are loaded syncronously, except when thumbnails are used, so it is best used for skin images. For images that require asyncronous loading
  /// (such as poster art) use MultiImage.
  /// </summary>
  public class BitmapImage : ImageSource
  {
    #region Private fields

    protected AbstractProperty _uriSourceProperty;
    protected AbstractProperty _decodePixelWidthProperty;
    protected AbstractProperty _decodePixelHeightProperty;
    protected AbstractProperty _borderColorProperty;
    protected AbstractProperty _effectProperty;
    protected AbstractProperty _effectTimerProperty;

    protected bool _thumbnail = false;
    protected TextureAsset _texture = null;
    protected PrimitiveBuffer _primitiveBuffer = new PrimitiveBuffer();
    protected ImageContext _imageContext = new ImageContext();
    protected Vector4 _frameData;

    #endregion

    #region Ctor

    public BitmapImage()
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
      _uriSourceProperty = new SProperty(typeof(string), null);
      _decodePixelWidthProperty = new SProperty(typeof(int), 0);
      _decodePixelHeightProperty = new SProperty(typeof(int), 0);
      _borderColorProperty = new SProperty(typeof(Color), Color.FromArgb(0, Color.Black));
      _effectProperty = new SProperty(typeof(string), null);
      _effectTimerProperty = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _uriSourceProperty.Attach(OnSourceChanged);
      _effectProperty.Attach(OnEffectChanged);
    }

    void Detach()
    {
      _uriSourceProperty.Detach(OnSourceChanged);
      _effectProperty.Detach(OnEffectChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Detach();
      BitmapImage b = (BitmapImage) source;
      UriSource = b.UriSource;
      DecodePixelWidth = b.DecodePixelWidth;
      DecodePixelHeight = b.DecodePixelHeight;
      Thumbnail = b.Thumbnail;
      BorderColor = b.BorderColor;
      Effect = b.Effect;
      EffectTimer = b.EffectTimer;
      
      Attach();
      FreeTextures();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the path to the image for this source.
    /// </summary>
    public string UriSource
    {
      get { return (string)_uriSourceProperty.GetValue(); }
      set { _uriSourceProperty.SetValue(value); }
    }

    public AbstractProperty UriSourceProperty
    {
      get { return _uriSourceProperty; }
    }

    /// <summary>
    /// Gets or sets a value that determines the width of the image stored in memory. 
    /// For large images this can decrease memory use and improve performance.
    /// To preserve the image's aspect ratio only set one of DecodeWidth or DecodeHeight.
    /// </summary>
    public int DecodePixelWidth
    {
      get { return (int) _decodePixelWidthProperty.GetValue(); }
      set { _decodePixelWidthProperty.SetValue(value); }
    }

    public AbstractProperty DecodePixelWidthProperty
    {
      get { return _decodePixelWidthProperty; }
    }

    /// <summary>
    /// Gets or sets a value that determines the width of the image stored in memory. 
    /// For large images this can decrease memory use and improve performance.
    /// To preserve the image's aspect ratio only set one of DecodeWidth or DecodeHeight.
    /// </summary>
    public int DecodePixelHeight
    {
      get { return (int) _decodePixelHeightProperty.GetValue(); }
      set { _decodePixelHeightProperty.SetValue(value); }
    }

    public AbstractProperty DecodePixelHeightProperty
    {
      get { return _decodePixelHeightProperty; }
    }

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
      get { return (string)_effectProperty.GetValue(); }
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
      get { return (double)_effectTimerProperty.GetValue(); }
      set { _effectTimerProperty.SetValue(value); }
    }

    public AbstractProperty EffectTimerProperty
    {
      get { return _effectTimerProperty; }
    }

    /// <summary>
    /// Gets or sets a value indicating that the image will be loaded as a thumbnail.
    /// </summary>
    /// <remarks>
    /// This is not an MPF accessible property. To set it use the Thumbnail property on the owner Image control.
    /// </remarks>
    public bool Thumbnail
    {
      get { return _thumbnail; }
      set { 
        if (value != _thumbnail) 
          FreeTextures(); 
        _thumbnail = value; 
      }
    }

    #endregion

    #region ImageSource implementation

    public override bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    public override SizeF SourceSize
    {
      get { return (_texture != null && _texture.IsAllocated) ? new SizeF(_texture.Width, _texture.Height) : new SizeF(); }
    }

    public override void Allocate()
    {
      string uri = UriSource;
      if (String.IsNullOrEmpty(uri))
      {
        if (_texture != null)
        {
          FreeTextures();
          FireChanged();
        }
        return;
      }
      if (_texture == null)
        _texture = ServiceRegistration.Get<ContentManager>().GetTexture(uri, DecodePixelWidth, DecodePixelHeight, Thumbnail);
      if (_texture != null && !_texture.IsAllocated)
      {
        _texture.Allocate();
        if (_texture.IsAllocated)
        {
          _frameData.X = (float)_texture.Width;
          _frameData.Y = (float)_texture.Height;
          _imageContext.Refresh();
          FireChanged();
        }
      }
    }

    public override void Deallocate()
    {
      _primitiveBuffer.Dispose();
      FreeTextures();
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

      _primitiveBuffer.Set(ref verts, PrimitiveType.TriangleFan);

      _imageContext.FrameSize = skinNeutralAR ? ImageContext.AdjustForSkinAR(ownerRect.Size) : ownerRect.Size;
    }

    public override void Render(RenderContext renderContext, Stretch stretchMode, StretchDirection stretchDirection)
    {
      SizeF sourceSize = StretchSource(_imageContext.FrameSize, new SizeF(_texture.Width, _texture.Height), stretchMode, stretchDirection);
      _frameData.Z = (float)EffectTimer;

      if (IsAllocated && _imageContext.StartRender(renderContext, sourceSize, _texture, BorderColor.ToArgb(), _frameData))
      {
        _primitiveBuffer.Render(0);
        _imageContext.EndRender();
      }
    }

    #endregion

    #region Protected methods

    protected virtual void OnSourceChanged(AbstractProperty prop, object oldValue)
    {
      FreeTextures();
      FireChanged();
    }

    protected virtual void OnEffectChanged(AbstractProperty prop, object oldValue)
    {
      _imageContext.ShaderEffect = Effect;
    }

    protected virtual void FreeTextures()
    {
      _texture = null;
      _imageContext.Clear();
    }

    #endregion
  }
}
