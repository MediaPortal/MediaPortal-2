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

using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  // It doesn't seem to be possible to create a texture which provides multisample surfaces. So for rendering to a surface/texture
  // with multisample antialiasing enabled, we must provide this special surface asset.
  public class RenderTargetAssetCore : TemporaryAssetCoreBase, IAssetCore
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    #region Protected fields

    protected Surface _surface = null;
    protected Size _size = new Size();
    protected Format _format = Format.A8B8G8R8;

    #endregion

    public Surface Surface
    {
      get
      {
        KeepAlive();
        return _surface;
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

    public void Allocate(int width, int height, Format format, MultisampleType multisampleType, int multisampleQuality, bool lockable)
    {
      bool free;
      lock (_syncObj)
        free = width != _size.Width || height != _size.Height || format != _format;
      if (free)
        Free();
      lock (_syncObj)
      {
        if (_surface != null)
          return;

        _size.Width = width;
        _size.Height = height;
        _format = format;

        _surface = Surface.CreateRenderTarget(GraphicsDevice.Device, width, height, format, multisampleType, multisampleQuality, lockable);
      }
      AllocationChanged(AllocationSize);
      KeepAlive();
    }

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return _surface != null; }
    }

    public int AllocationSize
    {
      get { return IsAllocated ? _size.Width * _size.Height * 4 : 0; }
    }

    public void Free()
    {
      if (_surface == null)
        return;
      AllocationChanged(-AllocationSize);
      _surface.Dispose();
      _surface = null;
      _size = new Size();
    }

    #endregion
  }
}