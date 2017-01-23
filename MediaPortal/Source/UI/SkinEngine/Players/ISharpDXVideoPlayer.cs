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

using MediaPortal.UI.Presentation.Players;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Players
{
  public delegate void RenderDlgt();

  /// <summary>
  /// Interface which has to be implemented by video players which are written for this SkinEngine.
  /// </summary>
  /// <remarks>
  /// Video players always have a tight coupling with the underlaying video rendering engine. So we need
  /// to provide a SkinEngine-specific interface for the rendering methods to support rendering of video
  /// players in the context of this SkinEngine.
  /// </remarks>
  public interface ISharpDXVideoPlayer : IVideoPlayer
  {
    /// <summary>
    /// Sets a delegate function which should be called for each frame from this player.
    /// </summary>
    /// <param name="dlgt">The render delegate function to call.</param>
    /// <returns><c>true</c>, if this player is able to call the given <paramref name="dlgt"/> for each frame. If this
    /// method returns <c>false</c>, the default render thread will continue to render, which might cause flickering.</returns>
    bool SetRenderDelegate(RenderDlgt dlgt);

    /// <summary>
    /// Returns the render texture for the current frame. May be <c>null</c>.
    /// </summary>
    Texture Texture { get; } 

    /// <summary>
    /// Gets the rectangle out of the video frame <see cref="Surface"/> which should be presented.
    /// </summary>
    /// <remarks>
    /// Video players may adjust the final video frame according to the <see cref="IVideoPlayer.CropSettings"/> in the
    /// <see cref="Surface"/> property or they may simply return a bigger frame, returning the crop rectangle in this property.
    /// In that case, the SkinEngine will crop the <see cref="Surface"/> by this rectangle.
    /// </remarks>
    Rectangle CropVideoRect { get; }

    /// <summary>
    /// Returns a mutex object to lock while accessing the <see cref="Texture"/>.
    /// </summary>
    object SurfaceLock { get; }

    /// <summary>
    /// Releases any GUI resources.
    /// </summary>
    void ReleaseGUIResources();

    /// <summary>
    /// Reallocs any GUI resources.
    /// </summary>
    void ReallocGUIResources();
  }

  /// <summary>
  /// Extended support for multiple texture planes.
  /// </summary>
  public interface ISharpDXMultiTexturePlayer : ISharpDXVideoPlayer
  {
    /// <summary>
    /// Returns additional texture planes that will be overlayed over the original <see cref="Texture"/>.
    /// This can be used for OSD (like BluRay player) or subtitles.
    /// </summary>
    Texture[] TexturePlanes { get; } 
  }
}
