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
using MediaPortal.Core.Properties;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls
{
  public class AlphaMask : Control, IAsset
  {
    #region variables

    private Texture _texture;
    private List<GradientStop> _gradientStops;
    private DateTime _lastTimeUsed = DateTime.MinValue;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AlphaMask"/> class.
    /// </summary>
    public AlphaMask()
      : base(null)
    {
      _gradientStops = new List<GradientStop>();
      _isVisible = new Property(true);
      Width = 512;
      Height = 512;
    }

    /// <summary>
    /// Createss the alphamask
    /// </summary>
    private void Create()
    {
      if (_texture != null)
      {
        Free();
      }
//      ServiceScope.Get<ILogger>().Debug("alphamask alloc texture ");
      _texture = new Texture(GraphicsDevice.Device, (int)_width, (int)_height, 0, Usage.None, Format.A8R8G8B8, Pool.Managed);
      int[,] buffer = (int[,])_texture.LockRectangle(typeof(int), 0, LockFlags.None, new int[] { (int)_width, (int)_height });

      ContentManager.TextureReferences++;
      for (int i = 0; i < _gradientStops.Count; ++i)
      {
        GradientStop stopbegin = _gradientStops[i];
        float alphaStart = stopbegin.Color;
        float alphaEnd = 1;
        int startY = (int) (stopbegin.Offset*((float) _height));
        int endY = (int)_height;
        if (i + 1 < _gradientStops.Count)
        {
          GradientStop stopend = _gradientStops[i + 1];
          alphaEnd = stopend.Color;
          endY = (int) (stopend.Offset*((float) _height));
        }

        float w = endY - startY;
        for (int y = startY; y < endY; ++y)
        {
          float alpha = (y - startY);
          alpha /= w;
          alpha *= (alphaEnd - alphaStart);
          alpha += alphaStart;
          ColorValue color = new ColorValue(1, 1, 1, alpha);
          for (int x = 0; x < _width; ++x)
          {
            buffer[y, x] = color.ToArgb();
          }
        }
      }

      _texture.UnlockRectangle(0);
      // TextureLoader.Save(@"c:\1.png", ImageFileFormat.Png, _texture);
    }

    /// <summary>
    /// Renders this alphamask.
    /// </summary>
    public void Render()
    {
      if (!IsAllocated)
      {
        Create();
      }
      GraphicsDevice.Device.SetTexture(1, _texture);
      _lastTimeUsed = SkinContext.Now;
    }

    /// <summary>
    /// Gets the gradient stops.
    /// </summary>
    /// <value>The gradient stops.</value>
    public List<GradientStop> GradientStops
    {
      get { return _gradientStops; }
    }


    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public bool IsAllocated
    {
      get { return (_texture != null); }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - _lastTimeUsed;
        if (ts.TotalSeconds >= 5)
        {
          return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Frees this asset.
    /// </summary>
    public void Free()
    {
      if (_texture != null)
      {
//        ServiceScope.Get<ILogger>().Debug("texture dispose alphamask");
        _texture.Dispose();
        _texture = null;

        ContentManager.TextureReferences--;
      }
    }

    #endregion
  }
}
