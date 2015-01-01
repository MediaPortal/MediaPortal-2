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

using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX.Direct2D1;
using Size = SharpDX.Size2;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class RenderTarget2DAssetCore : TemporaryAssetCoreBase, IAssetCore
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    #region Protected fields

    protected Bitmap1 _bitmap = null;
    protected Size _size = new Size();
    protected PixelFormat _format = new PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);

    #endregion

    public Bitmap1 Bitmap
    {
      get
      {
        KeepAlive();
        return _bitmap;
      }
    }

    public int Width
    {
      get { return _size.Width; }
    }

    public int Height
    {
      get { return _size.Height; }
    }

    public Size Size
    {
      get { return _size; }
    }

    public void Allocate(int width, int height, BitmapOptions options = BitmapOptions.Target)
    {
      bool free;
      lock (_syncObj)
        free = width != _size.Width || height != _size.Height;
      if (free)
        Free();
      lock (_syncObj)
      {
        if (_bitmap != null)
          return;

        _size.Width = width;
        _size.Height = height;

        // Create the D2D bitmap description using default 96 DPI
        var bitmapProperties = new BitmapProperties1(_format, 96, 96, options);

        // Create the render target
        _bitmap = new Bitmap1(GraphicsDevice11.Instance.Context2D1, new Size(width, height), bitmapProperties);
      }
      AllocationChanged(AllocationSize);
      KeepAlive();
    }

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return _bitmap != null; }
    }

    public int AllocationSize
    {
      get { return IsAllocated ? _size.Width * _size.Height * 4 : 0; }
    }

    public void Free()
    {
      if (_bitmap == null)
        return;
      AllocationChanged(-AllocationSize);
      _bitmap.Dispose();
      _bitmap = null;
      _size = new Size();
    }

    #endregion
  }
}
