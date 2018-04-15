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
  /// <summary>
  /// D3D settings which are compatible with the current hardware (adapter, device, mode, formats etc.)
  /// and which fulfills special criteria (e.g. best windowed mode, best fullscreen mode, ...)
  /// </summary>
  public class D3DConfiguration
  {
    /// <summary>
    /// The adapter of this configuration.
    /// </summary>
    public GraphicsAdapterInfo AdapterInfo;

    /// <summary>
    /// The device of this configuration.
    /// </summary>
    public GraphicsDeviceInfo DeviceInfo;

    /// <summary>
    /// The settings combination which is compatible with the <see cref="DeviceInfo"/>.
    /// </summary>
    public DeviceCombo DeviceCombo;

    /// <summary>
    /// The current display mode of the <see cref="AdapterInfo"/>.
    /// </summary>
    public DisplayMode DisplayMode;
  }
}
