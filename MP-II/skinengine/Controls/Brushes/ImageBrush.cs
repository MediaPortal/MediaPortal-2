#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion
using System;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;

namespace SkinEngine.Controls.Brushes
{
  public class ImageBrush : TileBrush
  {
    Property _imageSourceProperty;
    Property _downloadProgressProperty;
    TextureAsset _tex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageBrush"/> class.
    /// </summary>
    public ImageBrush()
    {
      Init();
    }
    public ImageBrush(ImageBrush b)
      : base(b)
    {
      Init();
      ImageSource = b.ImageSource;
    }
    void Init()
    {
      _imageSourceProperty = new Property(null);
      _downloadProgressProperty = new Property((double)0.0f);
      _imageSourceProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }
    public override object Clone()
    {
      return new ImageBrush(this);
    }

    /// <summary>
    /// Gets or sets the image source property.
    /// </summary>
    /// <value>The image source property.</value>
    public Property ImageSourceProperty
    {
      get
      {
        return _imageSourceProperty;
      }
      set
      {
        _imageSourceProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the image source.
    /// </summary>
    /// <value>The image source.</value>
    public string ImageSource
    {
      get
      {
        return (string)_imageSourceProperty.GetValue();
      }
      set
      {
        _imageSourceProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the download progress property.
    /// </summary>
    /// <value>The download progress property.</value>
    public Property DownloadProgressProperty
    {
      get
      {
        return _downloadProgressProperty;
      }
      set
      {
        _downloadProgressProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the download progress.
    /// </summary>
    /// <value>The download progress.</value>
    public double DownloadProgress
    {
      get
      {
        return (double)_downloadProgressProperty.GetValue();
      }
      set
      {
        _downloadProgressProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The prop.</param>
    protected override void OnPropertyChanged(Property prop)
    {
      Free();
    }

    /// <summary>
    /// Frees this instance.
    /// </summary>
    public void Free()
    {
      _tex = null;
    }

    /// <summary>
    /// Allocates this instance.
    /// </summary>
    public void Allocate()
    {
      bool thumb = true;
      _tex = ContentManager.GetTexture(ImageSource.ToString(), thumb);
      _tex.Allocate();
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="verts">The verts.</param>
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      if (_tex == null)
      {
        Allocate();
        base.SetupBrush(element, ref verts);
      }
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    public override void BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (_tex == null)
      {
        Allocate();
      }
      _tex.Set(0);
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public override void EndRender()
    {
      GraphicsDevice.Device.SetTexture(0, null);
    }

    /// <summary>
    /// Scales the specified u.
    /// </summary>
    /// <param name="u">The u.</param>
    /// <param name="v">The v.</param>
    protected override void Scale(ref float u, ref float v)
    {
      if (_tex == null) return;
      u *= _tex.MaxU;
      v *= _tex.MaxV;
    }

    /// <summary>
    /// Gets the brush dimensions.
    /// </summary>
    /// <value>The brush dimensions.</value>
    protected override Vector2 BrushDimensions
    {
      get
      {
        if (_tex == null)
          return base.BrushDimensions;
        return new Vector2(_tex.Width, _tex.Height);
      }
    }
  }
}
