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

using System.Collections.Generic;

namespace MediaPortal.UI.Presentation.Geometries
{
  /// <summary>
  /// Manages a set of <see cref="IGeometry"/> instances to do transformations for video windows, a current geometry
  /// and cropping settings and provides methods for doing video window transformations.
  /// </summary>
  public interface IGeometryManager
  {
    /// <summary>
    /// Gets or sets the default geometry to be used for playback of a new video.
    /// </summary>
    IGeometry DefaultVideoGeometry { get; set; }

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
    /// <param name="geometryName">The name of the geometry instance to remove.</param>
    void Remove(string geometryName);

    /// <summary>
    /// Returns a dictionary of available geometry instances. The dictionary contains geometry names mapped
    /// to geometry instances.
    /// </summary>
    IDictionary<string, IGeometry> AvailableGeometries { get; }

    /// <summary>
    /// Gets the relative name of the video effect file which represents the null-effect.
    /// </summary>
    string StandardEffectFile { get; }

    /// <summary>
    /// Returns a list of the available video/image effects. Relative file names are matched to human readable names.
    /// </summary>
    IDictionary<string, string> AvailableEffects { get; }
  }
}
