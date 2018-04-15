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
    string Shader { get; }

    /// <summary>
    /// Returns <c>true</c> if this geometry implementation wants it's input size 
    /// pre-scaled to the source's correct aspect ratio before transformation.
    /// </summary> 
    /// <remarks>For some input sources there will be a difference between the aspect 
    /// ratio of the actual frame and the desired output aspect. This property 
    /// indicates whether this <see cref="IGeometry"/> implementation requires the 
    /// difference to have been resolved before it's <see cref="Transform"/> function 
    /// is called. 
    /// 
    /// The primary example of content of this type is anamorphic widescreen format video, 
    /// used for storing widescreen video in 4:3 formats such as DVD.</remarks>
    bool RequiresCorrectAspectRatio { get; }

    /// <summary>
    /// Transforms an input size (representing the size of a video frame for instance) to 
    /// best fit into the given target size. The actual transformation performed depends 
    /// on the implementation. 
    /// </summary>
    /// <param name="inputSize">The size to be transformed.</param>
    /// <param name="targetSize">The size to fit the input into.</param>
    /// <returns>The input size tranformed to fit into <paramref name="targetSize>.</returns>
    SizeF Transform(SizeF inputSize, SizeF targetSize);
  }
}
