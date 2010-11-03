#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

namespace MediaPortal.UI.Presentation.Geometries
{
  /// <summary>
  /// Describes a video "zoom" mode. Contains methods to transform a given source rectangle to a destination rectangle
  /// by applying this geometry.
  /// </summary>
  public interface IGeometry
  {
    /// <summary>
    /// Gets the display name of this geometry.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the shader to use.
    /// </summary>
    string Shader { get;}

    /// <summary>
    /// Returns <c>true</c> if the source should be cropped.
    /// </summary>
    bool Crop { get; }

    /// <summary>
    /// Does the actual transformation of the given video area.
    /// </summary>
    /// <param name="inputSize">The original size of the input.</param>
    /// <param name="targetSize">The total size available.</param>
    /// TODO: More docs, document return value
    SizeF Transform(SizeF inputSize, SizeF targetSize);
  }
}
