#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System.Drawing;
using MediaPortal.UI.Presentation.Players;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Players
{
  public delegate void RenderDlgt();

  /// <summary>
  /// Interface which has to be implemented by video players, which are written for this SkinEngine.
  /// </summary>
  /// <remarks>
  /// Video players always have a tight coupling with the underlaying video rendering engine. So we need
  /// to provide a SkinEngine-specific interface for the rendering methods to support rendering of video
  /// players in the context of this SkinEngine.
  /// </remarks>
  public interface ISlimDXVideoPlayer : IVideoPlayer
  {
    /// <summary>
    /// Sets a delegate function which should be called for reach frame from this player.
    /// </summary>
    /// <param name="dlgt">The render delegate function to call.</param>
    /// <returns><c>true</c>, if this player is able to call the given <paramref name="dlgt"/> for each frame. If this
    /// method returns <c>false</c>, the default render thread will continue to render, which might cause flickering.</returns>
    bool SetRenderDelegate(RenderDlgt dlgt);

    /// <summary>
    /// Returns the maximum texture UV coords for displaying the complete frame. The frame texture must contain
    /// the cropped video image at its coordinates (0; 0).
    /// </summary>
    /// <remarks>
    /// Standard (legacy) DirectX requires that all textures have power-of-2 dimensions. When a texture is created of
    /// non-power-of-2 dimensions it's size is rounded up and an empty border is used to fill the extra space, which 
    /// means that the texture coordinates of the frame (without the border) within the texture are no longer [1.0, 1.0]. 
    /// 
    /// This function proivdes the correct texture coordinates to use to display the whole frame without any 
    /// border.
    /// </remarks>
    SizeF SurfaceMaxUV { get; }

    /// <summary>
    /// Returns the texture for the current frame. May be <c>null</c>.
    /// </summary>
    Texture Texture { get; } 

    /// <summary>
    /// Releases any GUI resources.
    /// </summary>
    void ReleaseGUIResources();

    /// <summary>
    /// Reallocs any GUI resources.
    /// </summary>
    void ReallocGUIResources();
  }
}