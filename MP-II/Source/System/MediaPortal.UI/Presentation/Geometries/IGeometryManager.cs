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

using System.Collections.Generic;
using System.Drawing;

namespace MediaPortal.Presentation.Geometries
{
  /// <summary>
  /// Manages a set of <see cref="IGeometry"/> instances to do transformations for video windows, a current geometry
  /// and cropping settings and provides methods for doing video window transformations.
  /// </summary>
  public interface IGeometryManager
  {
    /// <summary>
    /// Gets or sets the current geometry to be used for video playback.
    /// </summary>
    IGeometry CurrentVideoGeometry { get; set; }

    /// <summary>
    /// Gets or sets the current cropping settings.
    /// </summary>
    CropSettings CropSettings { get; set; }

    /// <summary>
    /// Adds a geometry to the set of available geometries.
    /// </summary>
    /// <param name="geometry">The geometry instance to add.</param>
    void Add(IGeometry geometry);

    /// <summary>
    /// Removes a geometry from the set of available geometries.
    /// </summary>
    /// <param name="geometry">The geometry instance to remove.</param>
    void Remove(IGeometry geometry);

    /// <summary>
    /// Returns the collection of available geometry instances.
    /// </summary>
    ICollection<IGeometry> AvailableGeometries { get;}

    /// <summary>
    /// This method will do the transformation based on the current geometry.
    /// It will calculate 2 rectangles: A source and destination rectangle based on the
    /// current geometry, video and target size.
    /// the returned source rectangle specifies which part of the image/video should be copied
    /// the returned destination rectangle specifies where the copied part should be presented on screen
    /// </summary>
    /// <param name="data">The data the transformation should be based on.</param>
    /// <param name="rSource">Rectangle specifying which part of the image/video should be copied.</param>
    /// <param name="rDest">Rectangle specifying to which part of the target rectangle (given in the
    /// <paramref name="data"/>) the copied part should be presented.</param>
    void Transform(GeometryData data, out Rectangle rSource, out Rectangle rDest);
  }
}
