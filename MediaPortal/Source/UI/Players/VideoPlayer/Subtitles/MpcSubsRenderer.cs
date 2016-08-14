#region Copyright (C) 2007-2016 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.IO;
using MediaPortal.UI.Players.Video.Tools;
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
    /// Array containing different texture planes.
    /// </summary>
    private readonly Texture[] _planes = new Texture[1];

    /// <summary>
    /// Lock for syncronising the texture update and rendering
    /// </summary>
    private readonly object _syncObj = new Object();

    private readonly Action _onTextureInvalidated;

    public MpcSubsRenderer(Action onTextureInvalidated)
    {
      _onTextureInvalidated = onTextureInvalidated;
      _planes[0] = new Texture(SkinContext.Device, SkinContext.BackBufferWidth, SkinContext.BackBufferHeight, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
    }

    public Texture[] TexturePlanes
    {
      get
      {
        lock (_syncObj)
        {
          return _planes;
        }
      }
    }

    private void FreeResources()
    {
      lock (_syncObj)
      {
        FilterGraphTools.TryDispose(ref _planes[0]);
      }
    }

    public void DrawItem()
    {
      try
      {
        lock (_syncObj)
        {
          using (new TemporaryRenderTarget(_planes[0]))
          {
            SkinContext.Device.Clear(ClearFlags.Target, ColorConverter.FromArgb(0, Color.Black), 1.0f, 0);
            MpcSubtitles.Render(0, 0, SkinContext.BackBufferWidth, SkinContext.BackBufferHeight);
          }
          //SaveTexture(_planes[0], 0);
        }
      }
      catch (Exception ex)
      {
        // BluRayPlayerBuilder.LogError(ex.ToString());
      }

      if (_onTextureInvalidated != null)
        _onTextureInvalidated();
    }

    private int n = 0;

    private void SaveTexture(Texture texture, int index)
    {
      using (var stream = BaseTexture.ToStream(texture, ImageFileFormat.Png))
      using (var sr = new BinaryReader(stream))
      using (var fs = new FileStream(string.Format("overlay_{0}_{1}.png", index, (n++)), FileMode.Create))
      using (var sw = new BinaryWriter(fs))
      {
        byte[] buffer = new byte[512];
        int bytesRead;
        while ((bytesRead = sr.Read(buffer, 0, buffer.Length)) > 0)
        {
          sw.Write(buffer, 0, bytesRead);
        }
      }
    }

    #region IDisposable Member

    public void Dispose()
    {
      FreeResources();
    }

    #endregion
  }
}
