#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Brushes
{
  public class ImageBrush : TileBrush
  {
    Property _imageSourceProperty;
    Property _downloadProgressProperty;
    TextureAsset _tex;

    public ImageBrush()
    {
      _imageSourceProperty = new Property(null);
      _downloadProgressProperty = new Property((double)0.0f);
      _imageSourceProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

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

    protected override void OnPropertyChanged(Property prop)
    {
      Free();
    }

    public void Free()
    {
      _tex = null;
    }

    public void Allocate()
    {
      bool thumb = true;
      _tex = ContentManager.GetTexture(ImageSource.ToString(), thumb);
      _tex.Allocate();
    }

    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      if (_tex == null)
      {
        Allocate();
        base.SetupBrush(element, ref verts);
      }
    }

    public override void BeginRender()
    {
      if (_tex == null)
      {
        Allocate();
      }
      _tex.Set(0);
    }

    public override void EndRender()
    {
      GraphicsDevice.Device.SetTexture(0, null);
    }

    protected override void Scale(ref float u, ref float v)
    {
      if (_tex == null) return;
      u *= _tex.MaxU;
      v *= _tex.MaxV;
    }

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
