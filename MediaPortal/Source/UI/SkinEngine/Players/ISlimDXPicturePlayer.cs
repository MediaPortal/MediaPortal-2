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
  /// <summary>
  /// Interface which has to be implemented by picture players which are written for this SkinEngine.
  /// </summary>
  public interface ISlimDXPicturePlayer : IPicturePlayer
  {
    /// <summary>
    /// Returns a synchronization mutex object to be acquired while accessing/changing the picture textures.
    /// </summary>
    object PicturesLock { get; }

    /// <summary>
    /// Returns the texture which contains the current picture.
    /// </summary>
    Texture CurrentPicture { get; }

    /// <summary>
    /// Returns the value of the U coord of the texture that defines the horizontal extent of the image.
    /// </summary>
    float MaxU { get; }

    /// <summary>
    /// Returns the value of the V coord of the texture that defines the vertical extent of the image.
    /// </summary>
    float MaxV { get; }
  }
}