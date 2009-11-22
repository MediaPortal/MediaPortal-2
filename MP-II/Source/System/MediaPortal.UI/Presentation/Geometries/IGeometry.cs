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
    /// Does the actual transformation of the given video area data based on the geometry <paramref name="data"/>
    /// and the specified <paramref name="cropSettings"/> and stores the results in the specified
    /// source rectangle <paramref name="rSource"/> and the specified destination rectangle <paramref name="rDest"/>.
    /// </summary>
    /// <param name="data">The geometry data on which the transformation should be based.</param>
    /// <param name="cropSettings">The cropping to be used.</param>
    /// <param name="rSource">Returns the source rectangle. This is the rectangle which should be copied from the
    /// source image.</param>
    /// <param name="rDest">Returns the destination rectangle. This is the rectangle to which the copied part should
    /// be presented in the target area.</param>
    void Transform(GeometryData data, CropSettings cropSettings, out Rectangle rSource, out Rectangle rDest);
  }
}
