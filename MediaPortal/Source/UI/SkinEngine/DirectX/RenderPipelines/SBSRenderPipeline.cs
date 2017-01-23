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

using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// <see cref="SBSRenderPipeline"/> implements a Side-By-Side rendering pipeline, where the first pass represents the left eye frame.
  /// </summary>
  internal class SBSRenderPipeline : AbstractMultiPassRenderPipeline
  {
    public override void BeginRender()
    {
      base.BeginRender();
      _firstFrameTargetRect = new Rectangle(0, 0, _renderTarget.Width / 2, _renderTarget.Height);
      _secondFrameTargetRect = new Rectangle(_renderTarget.Width / 2, 0, _renderTarget.Width / 2, _renderTarget.Height);
    }

    public override void GetVideoClip(RectangleF fullVideoClip, out RectangleF tranformedRect)
    {
      tranformedRect = GraphicsDevice.RenderPass == RenderPassType.SingleOrFirstPass ?
        new RectangleF(0.0f, 0.0f, fullVideoClip.Width * 0.5f, fullVideoClip.Height) : // SBS first pass, left side
        new RectangleF(fullVideoClip.Width * 0.5f, 0.0f, fullVideoClip.Width * 0.5f, fullVideoClip.Height); // SBS second pass, right side
    }

    public override Matrix GetRenderPassTransform(Matrix initialScreenTransform)
    {
      Matrix initialMatrix = initialScreenTransform;
      initialMatrix *= Matrix.Scaling(0.5f, 1, 1); // Scale to left side
      if (GraphicsDevice.RenderPass == RenderPassType.SecondPass)
        initialMatrix *= Matrix.Translation(SkinContext.WindowSize.Width * 0.5f, 0, 0); // Move to right side
      return initialMatrix;
    }
  }
}
