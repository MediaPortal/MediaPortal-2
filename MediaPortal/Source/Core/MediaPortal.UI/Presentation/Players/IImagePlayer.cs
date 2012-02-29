#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.UI.Presentation.Players
{
  public enum RightAngledRotation
  {
    Zero,
    HalfPi,
    Pi,
    ThreeHalfPi
  }

  /// <summary>
  /// Interface for image players. Holds all methods which are common to all image players.
  /// </summary>
  public interface IImagePlayer : IPlayer
  {
    IResourceLocator CurrentImageResourceLocator { get; }
    
    /// <summary>
    /// Returns the size of the whole image.
    /// </summary>
    Size ImageSize { get; }

    /// <summary>
    /// Returns the rotation which must be applied to the image.
    /// </summary>
    RightAngledRotation Rotation { get; }

    /// <summary>
    /// Returns the information whether the image must be flipped in horiziontal direction after the <see cref="Rotation"/> was applied.
    /// </summary>
    bool FlipX { get; }

    /// <summary>
    /// Returns the information whether the image must be flipped in vertical direction after the <see cref="Rotation"/> was applied.
    /// </summary>
    bool FlipY { get; }

    bool SlideShowEnabled { get; set; }
  }
}