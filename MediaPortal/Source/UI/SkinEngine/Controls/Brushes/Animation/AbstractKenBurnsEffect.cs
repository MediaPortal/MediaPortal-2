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

using System;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes.Animation
{
  /// <summary>
  /// Abstract base class for Ken Burns pan and zoom effect classes.
  /// </summary>
  public abstract class AbstractKenBurnsEffect
  {
    /// <summary>
    /// Randomizer which can be used by sub classes.
    /// </summary>
    protected static readonly Random _randomizer = new Random(DateTime.Now.Millisecond);

    /// <summary>
    /// Returns the information whether the image with the given <paramref name="imageSize"/> has landscape orientation
    /// in relation to the given <paramref name="outputSize"/>. A landscape image has borders at its top and bottom while
    /// a portrait image has borders at its left and right sides.
    /// </summary>
    /// <param name="imageSize">Size or aspect ratio of the image.</param>
    /// <param name="outputSize">Size or aspect ratio of the available output region to show the image.</param>
    /// <returns><c>true</c>, if the image has landscape orientation, else <c>false</c>.</returns>
    protected bool IsLandscape(SizeF imageSize, SizeF outputSize)
    {
      return imageSize.Width / outputSize.Width > imageSize.Height / outputSize.Height;
    }

    /// <summary>
    /// Returns the zoom view rectangle for the current animation state for the given <see cref="outputSize"/>.
    /// </summary>
    /// <param name="animationProgress">Progress of the animation, value between 0 (= start) and 1 (=end).</param>
    /// <param name="imageSize">Size of the image to animate.</param>
    /// <param name="outputSize">Size of the output region.</param>
    /// <returns>Rectangle which contains fractions of the image size; X and Y coords go from 0 to 1.</returns>
    public abstract RectangleF GetZoomRect(float animationProgress, Size imageSize, Size outputSize);
  }
}
