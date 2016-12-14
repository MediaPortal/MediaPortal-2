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
using Size = SharpDX.Size2;

namespace MediaPortal.UI.SkinEngine.Players
{
  /// <summary>
  /// Interface which has to be implemented by image players which are written for this SkinEngine.
  /// </summary>
  public interface ISharpDXImagePlayer : IImagePlayer
  {
    /// <summary>
    /// Returns a synchronization mutex object to be acquired while accessing/changing the image textures.
    /// </summary>
    object ImagesLock { get; }

    /// <summary>
    /// Returns the texture which contains the current image.
    /// </summary>
    Texture CurrentImage { get; }

    /// <summary>
    /// Returns the clipping region which should be taken fron the texture. Values go from 0 to 1.
    /// </summary>
    /// <param name="outputSize">Size of the output region.</param>
    RectangleF GetTextureClip(Size outputSize);
  }
}