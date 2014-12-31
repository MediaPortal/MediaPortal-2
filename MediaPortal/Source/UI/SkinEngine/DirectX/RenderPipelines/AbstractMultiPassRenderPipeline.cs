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

using MediaPortal.UI.SkinEngine.DirectX11;
using SharpDX;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// Abstract base class for multi-pass render pipelines.
  /// </summary>
  internal abstract class AbstractMultiPassRenderPipeline : AbstractRenderPipeline
  {
    protected Bitmap1 _backbuffer = null;
    protected Rectangle _renderRect;
    protected Rectangle _firstFrameTargetRect;
    protected Rectangle _secondFrameTargetRect;
    protected LayerParameters1 _layerParams1;
    protected LayerParameters1 _layerParams2;
    protected SolidColorBrush _opacityBrush;

    protected AbstractMultiPassRenderPipeline()
    {
      _backbuffer = GraphicsDevice11.Instance.RenderTarget2D;
    }

    protected void InitMasks()
    {
      _opacityBrush = new SolidColorBrush(GraphicsDevice11.Instance.Context2D1, Color4.Black);
      _layerParams1 = new LayerParameters1
      {
        ContentBounds = _firstFrameTargetRect,
        LayerOptions = LayerOptions1.None,
        MaskAntialiasMode = AntialiasMode.PerPrimitive,
        Opacity = 1.0f,
        OpacityBrush = _opacityBrush
      };
      _layerParams2 = new LayerParameters1
     {
       ContentBounds = _secondFrameTargetRect,
       LayerOptions = LayerOptions1.None,
       MaskAntialiasMode = AntialiasMode.PerPrimitive,
       Opacity = 1.0f,
       OpacityBrush = _opacityBrush
     };
    }

    public override void BeginRender()
    {
      // Remember current backbuffer and set internal surface as new render target.
      _renderRect = new Rectangle(0, 0, GraphicsDevice.Width, GraphicsDevice.Height);
      base.BeginRender();
    }

    public override void Render()
    {
      // First frame.
      // We use a layer with defined rect to make sure we don't overdraw the target rect
      GraphicsDevice11.Instance.RenderPass = RenderPassType.SingleOrFirstPass;
      GraphicsDevice11.Instance.Context2D1.PushLayer(_layerParams1, null);
      base.Render();
      GraphicsDevice11.Instance.Context2D1.PopLayer();

      // Second frame.
      // We use a layer with defined rect to make sure we don't overdraw the target rect
      GraphicsDevice11.Instance.RenderPass = RenderPassType.SecondPass;
      GraphicsDevice11.Instance.Context2D1.PushLayer(_layerParams2, null);
      base.Render();
      GraphicsDevice11.Instance.Context2D1.PopLayer();
    }

    public override void EndRender()
    {
      // Restore backbuffer as render target.
      GraphicsDevice11.Instance.Context2D1.Target = _backbuffer;
      base.EndRender();
    }

    public override void Dispose()
    {
      if (_opacityBrush != null)
        _opacityBrush.Dispose();
    }
  }
}
