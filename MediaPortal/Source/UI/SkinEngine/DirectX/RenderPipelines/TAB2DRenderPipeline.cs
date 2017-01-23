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

using SharpDX;

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// <see cref="TAB2DRenderPipeline"/> renders a Side-By-Side video into a 2D scene by cropping the source video to left side only.
  /// </summary>
  internal class TAB2DRenderPipeline : AbstractRenderPipeline
  {
    public override void GetVideoClip(RectangleF fullVideoClip, out RectangleF tranformedRect)
    {
      tranformedRect = new RectangleF(0.0f, 0.0f, fullVideoClip.Width, fullVideoClip.Height * 0.5f); // TAB first pass, upper side
    }
  }
}
