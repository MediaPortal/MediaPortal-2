#region Copyright (C) 2007-2015 Team MediaPortal

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

using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// Abstract render pipeline that implementes a generic 1-pass 2D rendering.
  /// </summary>
  internal abstract class AbstractRenderPipeline : IRenderPipeline
  {
    public virtual void BeginRender()
    {
      // TODO: this can't work that 2 different engines render concurrently to different devices
      //GraphicsDevice.RenderPass = RenderPassType.SingleOrFirstPass;
      //GraphicsDevice.Device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
      //GraphicsDevice.Device.BeginScene();
      GraphicsDevice11.Instance.RenderPass = RenderPassType.SingleOrFirstPass;
      GraphicsDevice11.Instance.Context2D1.BeginDraw();
      GraphicsDevice11.Instance.Context2D1.Clear(Color.Black);
    }

    public virtual void Render()
    {
      GraphicsDevice.ScreenManager.Render();
    }

    public virtual void EndRender()
    {
      //GraphicsDevice.Device.EndScene();
      GraphicsDevice11.Instance.Context2D1.EndDraw();
    }

    public virtual void GetVideoClip(RectangleF fullVideoClip, out RectangleF tranformedRect)
    {
      tranformedRect = fullVideoClip;
    }

    public virtual Matrix GetRenderPassTransform(Matrix initialScreenTransform)
    {
      return initialScreenTransform;
    }
  }
}
