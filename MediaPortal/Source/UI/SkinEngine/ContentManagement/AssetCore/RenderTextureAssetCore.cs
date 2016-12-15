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
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class RenderTextureAssetCore : TemporaryAssetCoreBase, ITextureAsset, IAssetCore
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    #region Protected fields

    protected Texture _texture = null;
    protected Surface _surface0 = null;
    protected Size _size = new Size();
    protected Usage _usage = Usage.RenderTarget;
    protected Format _format = Format.A8B8G8R8;
    protected float _maxU = 1.0f;
    protected float _maxV = 1.0f;

    #endregion

    public Texture Texture
    {
      get
      {
        KeepAlive();
        return _texture;
      }
    }

    public Surface Surface0
    {
      get
      {
        KeepAlive();
        return _surface0;
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

    public float MaxU
    {
      get { return _maxU; }
    }

    public float MaxV
    {
      get { return _maxV; }
    }

    public void Allocate(int width, int height, Usage usage, Format format)
    {
      bool free;
      lock (_syncObj)
        free = width != _size.Width || height != _size.Height || usage != _usage || format != _format;
      if (free)
        Free();
      lock (_syncObj)
      {
        if (_texture != null)
          return;

        _size.Width = width;
        _size.Height = height;
        _usage = usage;
        _format = format;

        // Note that it doesn't seem to be possible to create a texture with multisample surfaces inside. So rendering to that texture
        // won't provide multisample antialiasing.
        _texture = new Texture(GraphicsDevice.Device, width, height, 1, usage, format, Pool.Default);
        _surface0 = _texture.GetSurfaceLevel(0);

        SurfaceDescription desc = _texture.GetLevelDescription(0);
        _maxU = _size.Width/((float) desc.Width);
        _maxV = _size.Height/((float) desc.Height);
      }
      AllocationChanged(AllocationSize);
      KeepAlive();
    }

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return _texture != null; }
    }

    public int AllocationSize
    {
      get { return IsAllocated ? _size.Width * _size.Height * 4 : 0; }
    }

    public void Free()
    {
      if (_texture == null)
        return;
      lock (_texture)
      {
        AllocationChanged(-AllocationSize);
        _surface0.Dispose();
        _texture.Dispose();
        _texture = null;
        _surface0 = null;
      }
      _size = new Size();
    }

    #endregion
  }
}