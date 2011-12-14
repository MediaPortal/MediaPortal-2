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

namespace MediaPortal.UI.Players.Picture.Animation
{
  /// <summary>
  /// Used for picture animation effects in the <see cref="PicturePlayer"/>.
  /// </summary>
  public interface IPictureAnimator
  {
    /// <summary>
    /// Returns the zoom view rectangle, X and Y coords go from 0 to 1.
    /// </summary>
    RectangleF ZoomRect { get; }

    /// <summary>
    /// Initializes the animation.
    /// </summary>
    /// <param name="imageSize">Size of the image to animate.</param>
    void Initialize(Size imageSize);

    /// <summary>
    /// Executes the animation and updates the final display rectangle.
    /// </summary>
     void Animate();
  }
}
