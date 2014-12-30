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

using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// Abstract base class for multi-pass render pipelines.
  /// </summary>
  internal abstract class AbstractMultiPassRenderPipeline : AbstractRenderPipeline
  {
    protected const string GLOBAL_RENDER_TARGET_ASSET_KEY = "SkinEngine::GlobalRenderTarget";
    protected RenderTarget2DAsset _renderTarget = null;
    protected Bitmap1 _backbuffer = null;
    protected Rectangle _renderRect;
    protected Rectangle _firstFrameTargetRect;
    protected RectangleGeometry _firstFrameGeometry;
    protected Rectangle _secondFrameTargetRect;
    protected RectangleGeometry _secondFrameGeometry;
    protected ImageBrushProperties _imageBrushProperties;
    protected ImageBrush _imgBrush;

    protected AbstractMultiPassRenderPipeline()
    {
      _backbuffer = GraphicsDevice11.Instance.RenderTarget2D;
      _renderTarget = ContentManager.Instance.GetRenderTarget2D(GLOBAL_RENDER_TARGET_ASSET_KEY);
      int width = GraphicsDevice11.Instance.BackBuffer.Description.Width;
      int height = GraphicsDevice11.Instance.BackBuffer.Description.Height;
      _renderTarget.AllocateRenderTarget(width, height);
      _imageBrushProperties = new ImageBrushProperties
      {
        ExtendModeX = ExtendMode.Clamp,
        ExtendModeY = ExtendMode.Clamp,
        InterpolationMode = InterpolationMode.Linear
      };
      _imgBrush = new ImageBrush(GraphicsDevice11.Instance.Context2D1, _renderTarget.Bitmap, _imageBrushProperties);
    }

    protected void InitGeometries()
    {
      _firstFrameGeometry = new RectangleGeometry(GraphicsDevice11.Instance.Context2D1.Factory, _firstFrameTargetRect);
      _secondFrameGeometry = new RectangleGeometry(GraphicsDevice11.Instance.Context2D1.Factory, _secondFrameTargetRect);
    }

    public override void BeginRender()
    {
      // Remember current backbuffer and set internal surface as new render target.
      _renderRect = new Rectangle(0, 0, GraphicsDevice.Width, GraphicsDevice.Height);
      GraphicsDevice11.Instance.Context2D1.Target = _renderTarget.Bitmap;
      base.BeginRender();
    }

    public override void Render()
    {
      // First frame.
      base.Render();
      CopyFirstFrameToBackbuffer();

      // Second frame.
      GraphicsDevice.RenderPass = RenderPassType.SecondPass;
      base.Render();
      CopySecondFrameToBackbuffer();
    }

    public override void EndRender()
    {
      // Restore backbuffer as render target.
      GraphicsDevice11.Instance.Context2D1.Target = _backbuffer;
      base.EndRender();
    }

    protected virtual void CopyFirstFrameToBackbuffer()
    {
      CopySubRegion(_firstFrameGeometry);
    }

    protected virtual void CopySecondFrameToBackbuffer()
    {
      CopySubRegion(_secondFrameGeometry);
    }

    private void CopySubRegion(RectangleGeometry geometry)
    {
      // Make sure all rendering is done, before we copy results
      GraphicsDevice11.Instance.Context2D1.Flush();

      // Transform brush into control scope
      Matrix3x2 transform = Matrix3x2.Identity;
      float contentWidth = _renderTarget.Width;
      float contentHeight = _renderTarget.Height;
      transform *= Matrix3x2.Scaling(_firstFrameGeometry.Rectangle.Width / contentWidth, _firstFrameGeometry.Rectangle.Height / contentHeight);
      transform *= Matrix3x2.Translation(_firstFrameGeometry.Rectangle.X, _firstFrameGeometry.Rectangle.Y);
      _imgBrush.Transform = transform;

      using (new TemporaryRenderTarget2D(_backbuffer))
        GraphicsDevice11.Instance.Context2D1.FillGeometry(geometry, _imgBrush);
    }

    public override void Dispose()
    {
      if (_imgBrush != null)
        _imgBrush.Dispose();
      if (_firstFrameGeometry != null)
        _firstFrameGeometry.Dispose();
      if (_secondFrameGeometry != null)
        _secondFrameGeometry.Dispose();
    }
  }
}
