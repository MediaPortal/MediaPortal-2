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
using System;
using System.Threading;
using SharpDX.DXGI;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderStrategy
{
  /// <summary>
  /// Default strategy uses <see cref="PresentFlags.DoNotWait"/> with manual frame time waiting.
  /// </summary>
  class Default : AbstractStrategy
  {
    protected DateTime _frameRenderingStartTime = DateTime.MinValue;
    private readonly bool _manualWaitFrame;

    public Default(D3DSetup setup) : base(setup)
    {
      _manualWaitFrame = true;
      PresentFlags = PresentFlags.None;
    }

    public override void BeginRender(bool doWaitForNextFame)
    {
      base.BeginRender(doWaitForNextFame);

      if (_frameRenderingStartTime != DateTime.MinValue && _manualWaitFrame && doWaitForNextFame)
        WaitForNextFrame();

      _frameRenderingStartTime = DateTime.Now;
    }

    /// <summary>
    /// Waits for the next frame to be drawn. It calculates the required difference to fit the <see cref="AbstractStrategy.TargetFrameRate"/>.
    /// </summary>
    private void WaitForNextFrame()
    {
      double msToNextFrame = _msPerFrame - (DateTime.Now - _frameRenderingStartTime).Milliseconds;
      if (msToNextFrame > 0)
        Thread.Sleep(TimeSpan.FromMilliseconds(msToNextFrame));
    }
  }
}
