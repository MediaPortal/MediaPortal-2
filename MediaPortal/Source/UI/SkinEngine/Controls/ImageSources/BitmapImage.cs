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

using System;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  public class BitmapImage : ImageSource
  {
    #region Consts

    protected const string EFFECT_NAME = "normal";

    protected const string PARAM_OPACITY = "g_opacity";
    protected const string PARAM_ZORDER = "g_zorder";

    #endregion

    #region Private fields

    protected AbstractProperty _uriSourceProperty;
    protected AbstractProperty _decodePixelWidthProperty;
    protected AbstractProperty _decodePixelHeightProperty;
    protected AbstractProperty _thumbnailProperty;
    protected AbstractProperty _shapeProperty;
    protected TextureAsset _texture0 = null;
    protected EffectAsset _effect = null;
    protected PrimitiveBuffer _geometry = new PrimitiveBuffer();

    protected RectangleF _oldRect;
    protected Stretch _oldStretchMode;
    protected StretchDirection _oldStretchDirection;
    protected bool _needsSetup = true;

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
      _thumbnailProperty = new SProperty(typeof(bool), false);
      _shapeProperty = new SProperty(typeof(Shape), null);
    }

    void Attach()
    {
      _uriSourceProperty.Attach(OnSourceChanged);
      _decodePixelWidthProperty.Attach(OnSourceChanged);
      _decodePixelHeightProperty.Attach(OnSourceChanged);
      _thumbnailProperty.Attach(OnSourceChanged);
      _shapeProperty.Attach(OnSourceChanged);
    }

    void Detach()
    {
      _uriSourceProperty.Detach(OnSourceChanged);
      _decodePixelWidthProperty.Detach(OnSourceChanged);
      _decodePixelHeightProperty.Detach(OnSourceChanged);
      _thumbnailProperty.Detach(OnSourceChanged);
      _shapeProperty.Detach(OnSourceChanged);
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
      Shape = b.Shape;
      Attach();
      FreeTextures();
    }

    #endregion

    #region Public properties

    public AbstractProperty UriSourceProperty
    {
      get { return _uriSourceProperty; }
    }

    /// <summary>
    /// Gets or sets the path to the image for this source.
    /// </summary>
    public string UriSource
    {
      get { return (string) _uriSourceProperty.GetValue(); }
      set { _uriSourceProperty.SetValue(value); }
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
    public int DecodePixelWidth
    {
      get { return (int) _decodePixelWidthProperty.GetValue(); }
      set { _decodePixelWidthProperty.SetValue(value); }
    }

    public AbstractProperty DecodePixelHeightProperty
    {
      get { return _decodePixelHeightProperty; }
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

    public AbstractProperty ThumbnailProperty
    {
      get { return _thumbnailProperty; }
    }

    /// <summary>
    /// Gets or sets a value indicating that the image will be loaded as a thumbnail.
    /// </summary>
    public bool Thumbnail
    {
      get { return (bool) _thumbnailProperty.GetValue(); }
      set { _thumbnailProperty.SetValue(value); }
    }

    public AbstractProperty ShapeProperty
    {
      get { return _shapeProperty; }
    }

    /// <summary>
    /// Gets or sets a <see cref="Shape"/> objects to be used as the base geometry when drawing the image.
    /// A normal rectangle will be used if this property is not set.
    /// </summary>
    public Shape Shape
    {
      get { return (Shape) _shapeProperty.GetValue(); }
      set { _shapeProperty.SetValue(value); }
    }

    #endregion

    #region ImageSource implementation

    public override bool IsAllocated
    {
      get { return _texture0 != null && _texture0.IsAllocated; }
    }

    public override SizeF SourceSize
    {
      get { return (_texture0 != null && _texture0.IsAllocated) ? new SizeF(_texture0.Width, _texture0.Height) : new SizeF(); }
    }

    public override void Allocate()
    {
      string uri = UriSource;
      if (String.IsNullOrEmpty(uri))
      {
        if (_texture0 != null)
        {
          FreeTextures();
          FireChanged();
          _needsSetup = true;
        }
        return;
      }
      if (_texture0 == null)
        _texture0 = Thumbnail ? ServiceRegistration.Get<ContentManager>().GetTexture(uri, Thumbnail) :
            ServiceRegistration.Get<ContentManager>().GetTexture(uri, DecodePixelWidth, DecodePixelHeight);
      if (_texture0 != null && !_texture0.IsAllocated)
      {
        _texture0.Allocate();
        if (_texture0.IsAllocated)
        {
          _needsSetup = true;
          FireChanged();
        }
      }
    }

    public override void Deallocate()
    {
      _geometry.Dispose();
      FreeTextures();
    }    

    public override void Render(RenderContext renderContext, RectangleF ownerRect, Stretch stretchMode, StretchDirection stretchDirection)
    {
      if (IsAllocated)
      {
        _needsSetup |= _oldRect != ownerRect || _oldStretchMode != stretchMode || _oldStretchDirection != stretchDirection;
        if (_needsSetup)
        {
          _oldRect = ownerRect;
          _oldStretchMode = stretchMode;
          _oldStretchDirection = stretchDirection;
          Setup(ownerRect, stretchMode, stretchDirection);
        }
        if (_effect != null)
        {
          // Render
          _effect.StartRender(_texture0.Texture, renderContext.Transform);
          _geometry.Render(0);
          _effect.EndRender();
        }
      }
    }

    #endregion

    #region Protected methods

    protected void Setup(RectangleF ownerRect, Stretch stretchMode, StretchDirection stretchDirection)
    {
      // Get actual image size
      SizeF ownerSize = ownerRect.Size;
      SizeF size0 = StretchSource(ownerRect.Size, new SizeF(_texture0.Width, _texture0.Height), stretchMode, stretchDirection);    
      PositionColored2Textured[] verts = null;
      PositionColored2Textured tl = new PositionColored2Textured();
      PositionColored2Textured br = new PositionColored2Textured();

      // Find actual image corners
      tl.X = Math.Max(ownerSize.Width - size0.Width, 0.0f) / 2 + ownerRect.X;
      tl.Y = Math.Max(ownerSize.Height - size0.Height, 0.0f) / 2 + ownerRect.Y;
      tl.Z = 1.0f;
      tl.Tu1 = (Math.Max(size0.Width - ownerSize.Width, 0.0f) / (2 * size0.Width)) * _texture0.MaxU;
      tl.Tv1 = (Math.Max(size0.Height - ownerSize.Height, 0.0f) / (2 * size0.Height)) * _texture0.MaxV;
      unchecked {
        tl.Color = (int) 0xFFFFFFFF;
      }

      // Note: BR is actually width/height here
      br.X = Math.Min(ownerSize.Width, size0.Width);
      br.Y = Math.Min(ownerSize.Height, size0.Height);
      br.Z = 1.0f;
      br.Tu1 = Math.Max(ownerSize.Height / size0.Width, 1.0f) * _texture0.MaxU;
      br.Tv1 = Math.Max(ownerSize.Height / size0.Width, 1.0f) * _texture0.MaxV;
      br.Color = tl.Color;

      // Use shape geometry if available
      if (Shape != null)
      {
        RectangleF rect = new RectangleF(tl.X, tl.Y, br.X, br.Y);
        verts = Shape.GetGeometry(rect);
        if (verts != null)
        {
          // Scale texture coords to match stretching
          for (int i = 0; i < verts.Length; i++)
          {
            verts[i].Tu1 = (verts[i].X / br.X) * br.Tu1 + tl.Tu1;
            verts[i].Tv1 = (verts[i].X / br.X) * br.Tv1 + tl.Tv1;
          }
        }
      }

      // If no shape geometry use a normal rectangle
      if (verts == null)
      {
        // Convert BR from width height to bottom-right corner
        br.X += tl.X;
        br.Y += tl.Y;
        br.Tu1 += tl.Tu1;
        br.Tv1 += tl.Tv1;

        verts = new PositionColored2Textured[4];
        // Top-left
        verts[0] = tl;
        // Bottom-left
        verts[1] = new PositionColored2Textured(tl.X, br.Y, 1.0f, tl.Tu1, br.Tv1, tl.Color);
        // Bottom-right
        verts[2] = br;
        // Top-right
        verts[3] = new PositionColored2Textured(br.X, tl.Y, 1.0f, br.Tu1, tl.Tv1, tl.Color);
      }
      // Create buffer and allocate effect
      _geometry.Set(ref verts, PrimitiveType.TriangleFan);
      _effect = ServiceRegistration.Get<ContentManager>().GetEffect(EFFECT_NAME);
      _needsSetup = false;
    }

    protected void OnSourceChanged(AbstractProperty prop, object oldValue)
    {
      FreeTextures();
    }

    protected void FreeTextures()
    {
      _texture0 = null;
    }

    #endregion
  }
}
