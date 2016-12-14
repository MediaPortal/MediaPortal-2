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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class MpcSubsRenderer : IDisposable
  {
    /// <summary>
    /// Lock for syncronising the texture update and rendering
    /// </summary>
    private readonly object _syncObj = new Object();

    private readonly Action _onTextureInvalidated;

    public MpcSubsRenderer(Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
    }

    public void DrawItem(Texture targetTexture, bool clear)
    {
      try
      {
        lock (_syncObj)
        {
          using (new TemporaryRenderTarget(targetTexture))
          {
            if (clear)
              SkinContext.Device.Clear(ClearFlags.Target, ColorConverter.FromArgb(0, Color.Black), 1.0f, 0);
            var surfaceDesc = targetTexture.GetLevelDescription(0);
            MpcSubtitles.Render(0, 0, surfaceDesc.Width, surfaceDesc.Height);
          }
        }
        if (_onTextureInvalidated != null)
          _onTextureInvalidated();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("MpcSubs: Error rendering subtitle onto video frame", ex);
      }
    }

    #region IDisposable Member

    public void Dispose()
    {
    }

    #endregion
  }
}
