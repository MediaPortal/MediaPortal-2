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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class RenderTargetAsset : AssetWrapper<RenderTargetAssetCore>
  {
    public RenderTargetAsset(RenderTargetAssetCore core) : base(core) { }

    /// <summary>
    /// Gets the underlaying render target surface resource.
    /// </summary>
    public Surface Surface
    {
      get { return _assetCore.Surface; }
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
      AllocateCustom(width, height, Format.A8R8G8B8, GraphicsDevice.Setup.PresentParameters.MultiSampleType,
          GraphicsDevice.Setup.PresentParameters.MultiSampleQuality, false);
    }

    /// <summary>
    /// Allocates a new render-texture with the specified parameters.
    /// </summary>
    public void AllocateCustom(int width, int height, Format format, MultisampleType multisampleType,
        int multisampleQuality, bool lockable)
    {
      _assetCore.Allocate(width, height, format, multisampleType, multisampleQuality, lockable);
    }
  }
}
