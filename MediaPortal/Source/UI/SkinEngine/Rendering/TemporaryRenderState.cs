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

using System;
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// <see cref="TemporaryRenderState"/> allows modifying the RenderState and automatically restoring
  /// all changes at the end (in Dispose). Use <see cref="SetTemporaryRenderState"/> to change temporary
  /// RenderStates.
  /// </summary>
  public class TemporaryRenderState : IDisposable
  {
    // The current D3D device
    private readonly DeviceEx _device = GraphicsDevice.Device;

    private readonly Dictionary<RenderState, int> _changedStates = new Dictionary<RenderState, int>();

    public void SetTemporaryRenderState(RenderState renderState, int value)
    {
      int oldState = _device.GetRenderState(renderState);
      if (oldState.Equals(value))
        return;

      _changedStates[renderState] = oldState;
      _device.SetRenderState(renderState, value);
    }

    public void Dispose()
    {
      foreach (KeyValuePair<RenderState, int> changedState in _changedStates)
      {
        _device.SetRenderState(changedState.Key, changedState.Value);
      }
    }
  }
}
