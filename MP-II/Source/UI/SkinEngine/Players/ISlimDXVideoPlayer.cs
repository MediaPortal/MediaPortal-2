#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.SkinEngine.Effects;

namespace MediaPortal.UI.SkinEngine.Players
{
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
    /// Releases any GUI resources.
    /// </summary>
    void ReleaseGUIResources();

    /// <summary>
    /// Reallocs any GUI resources.
    /// </summary>
    void ReallocGUIResources();

    // TODO: Tidy up from here

    void BeginRender(EffectAsset effect);
    void EndRender(EffectAsset effect);
  }
}