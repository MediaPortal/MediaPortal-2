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

using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX
{
  public class DxCapabilities
  {
    protected int _maxAnisotropy;
    protected bool _supportsFiltering;
    protected bool _supportsAlphaBlend;
    protected bool _supportsShaders;

    private DxCapabilities(int maxAnisotropy, bool supportsFiltering, bool supportsAlphaBlend, bool supportsShaders)
    {
      _maxAnisotropy = maxAnisotropy;
      _supportsFiltering = supportsFiltering;
      _supportsAlphaBlend = supportsAlphaBlend;
      _supportsShaders = supportsShaders;
    }

    public int MaxAnisotropy
    {
      get { return _maxAnisotropy; }
    }

    public bool SupportsFiltering
    {
      get { return _supportsFiltering; }
    }

    public bool SupportsAlphaBlend
    {
      get { return _supportsAlphaBlend; }
    }

    public bool SupportsShaders
    {
      get { return _supportsShaders; }
    }

    public static DxCapabilities RequestCapabilities(Capabilities deviceCapabilities, DisplayMode displayMode)
    {
      int maxAnisotropy = deviceCapabilities.MaxAnisotropy;
      bool supportsFiltering = MPDirect3D.Direct3D.CheckDeviceFormat(
          deviceCapabilities.AdapterOrdinal, deviceCapabilities.DeviceType, displayMode.Format,
          Usage.RenderTarget | Usage.QueryFilter, ResourceType.Texture, Format.A8R8G8B8);

      bool supportsAlphaBlend = MPDirect3D.Direct3D.CheckDeviceFormat(deviceCapabilities.AdapterOrdinal,
          deviceCapabilities.DeviceType, displayMode.Format, Usage.RenderTarget | Usage.QueryPostPixelShaderBlending,
          ResourceType.Surface, Format.A8R8G8B8);
      bool supportsShaders = deviceCapabilities.PixelShaderVersion.Major >= 2 && deviceCapabilities.VertexShaderVersion.Major >= 2;
      return new DxCapabilities(maxAnisotropy, supportsFiltering, supportsAlphaBlend, supportsShaders);
    }
  }
}