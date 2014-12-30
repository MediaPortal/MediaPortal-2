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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX.Direct2D1;
using Size = SharpDX.Size2;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class RenderTarget2DAsset : AssetWrapper<RenderTarget2DAssetCore>, IBitmapAsset2D
  {
    public RenderTarget2DAsset(RenderTarget2DAssetCore core) : base(core) { }

    /// <summary>
    /// Gets the actual Direct2D BitmapSource resource.
    /// </summary>
    public Bitmap1 Bitmap
    {
      get { return _assetCore.Bitmap; }
    }

    /// <summary>
    /// Gets the width of underlaying surface.
    /// </summary>
    public int Width
    {
      get { return _assetCore.Width; }
    }

    /// <summary>
    /// Get the height of underlaying surface.
    /// </summary>
    public int Height
    {
      get { return _assetCore.Height; }
    }

    /// <summary>
    /// Gets the size of the underlaying surface.
    /// </summary>
    public Size Size
    {
      get { return _assetCore.Size; }
    }

    /// <summary>
    /// Allocates a new render-texture with the specified size and default format.
    /// </summary>
    public void AllocateRenderTarget(int width, int height)
    {
      _assetCore.Allocate(width, height);
    }

    public void Allocate()
    {
      _assetCore.Allocate(SkinContext.BackBufferWidth, SkinContext.BackBufferHeight);
    }
  }
}
