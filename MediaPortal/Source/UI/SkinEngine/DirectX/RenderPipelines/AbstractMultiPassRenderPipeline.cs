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

using MediaPortal.UI.SkinEngine.ContentManagement;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// Abstract base class for multi-pass render pipelines.
  /// </summary>
  internal abstract class AbstractMultiPassRenderPipeline : AbstractRenderPipeline
  {
    protected const string GLOBAL_RENDER_SURFACE_ASSET_KEY = "SkinEngine::GlobalRenderSurface";
    protected RenderTargetAsset _renderTarget = null;
    protected Surface _backbuffer = null;
    protected Rectangle _renderRect;
    protected Rectangle _firstFrameTargetRect;
    protected Rectangle _secondFrameTargetRect;

    public override void BeginRender()
    {
      // Remember current backbuffer and set internal surface as new render target.
      _backbuffer = GraphicsDevice.Device.GetRenderTarget(0);
      _renderTarget = ContentManager.Instance.GetRenderTarget(GLOBAL_RENDER_SURFACE_ASSET_KEY);
      _renderTarget.AllocateRenderTarget(GraphicsDevice.Width, GraphicsDevice.Height);
      _renderRect = new Rectangle(0, 0, GraphicsDevice.Width, GraphicsDevice.Height);
      GraphicsDevice.Device.SetRenderTarget(0, _renderTarget.Surface);
      GraphicsDevice.RenderPass = RenderPassType.SingleOrFirstPass;
      GraphicsDevice.Device.Clear(ClearFlags.Target, Color.Black, 1.0f, 0);
      GraphicsDevice.Device.BeginScene();
    }

    public override void Render()
    {
      // First frame.
      base.Render();
      CopyFirstFrameToBackbuffer();
      GraphicsDevice.Device.EndScene();

      // Second frame.
      GraphicsDevice.RenderPass = RenderPassType.SecondPass;
      GraphicsDevice.Device.BeginScene();
      base.Render();
      CopySecondFrameToBackbuffer();
    }

    public override void EndRender()
    {
      // Restore backbuffer as render target.
      using (_backbuffer)
        GraphicsDevice.Device.SetRenderTarget(0, _backbuffer);
      base.EndRender();
    }

    protected virtual void CopyFirstFrameToBackbuffer()
    {
      GraphicsDevice.Device.StretchRectangle(_renderTarget.Surface, _firstFrameTargetRect, _backbuffer, _firstFrameTargetRect, TextureFilter.None);
    }

    protected virtual void CopySecondFrameToBackbuffer()
    {
      GraphicsDevice.Device.StretchRectangle(_renderTarget.Surface, _secondFrameTargetRect, _backbuffer, _secondFrameTargetRect, TextureFilter.None);
    }
  }
}
