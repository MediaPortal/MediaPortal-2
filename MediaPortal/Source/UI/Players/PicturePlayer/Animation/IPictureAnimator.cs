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
using SlimDX.Direct3D9;

namespace MediaPortal.UI.Players.Picture.Animation
{
  /// <summary>
  /// IPictureAnimator is used for picture animation effects used by <see cref="PicturePlayer"/>.
  /// </summary>
  public interface IPictureAnimator
  {
    /// <summary>
    /// Executes the animation and returns the final display rectangle for the given <paramref name="currentImage"/>.
    /// </summary>
    /// <param name="currentImage">Image to animate</param>
    /// <param name="maxUV">Max UV coordinates in texture</param>
    /// <returns>Zoom view rectangle</returns>
    RectangleF Animate(Texture currentImage, SizeF maxUV);

    /// <summary>
    /// Resets the animation.
    /// </summary>
    void Reset();
  }
}
