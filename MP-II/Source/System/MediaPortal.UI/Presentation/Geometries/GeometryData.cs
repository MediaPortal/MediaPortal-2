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
  /// Stores video size, target size and the pixel ratio for geometry transformations.
  /// </summary>
  public struct GeometryData
  {
    #region Private fields

    private readonly Size _originalSize;
    private readonly Size _targetSize;
    private readonly float _pixelRatio;

    #endregion

    /// <summary>
    /// Creates a new <see cref="GeometryData"/> instance.
    /// </summary>
    /// <param name="originalSize">The original size of a video or image source.</param>
    /// <param name="targetSize">The size of the target area where the video or image should be shown.</param>
    /// <param name="pixelRatio">The pixel ratio to be used.</param>
    public GeometryData(Size originalSize, Size targetSize, float pixelRatio)
    {
      _originalSize = originalSize;
      _targetSize = targetSize;
      _pixelRatio = pixelRatio;
    }

    /// <summary>
    /// Gets the original size of the video or image source.
    /// </summary>
    public Size OriginalSize
    {
      get { return _originalSize; }
    }

    /// <summary>
    /// Gets the target size of the area where the video or image should be shown.
    /// </summary>
    public Size TargetSize
    {
      get { return _targetSize; }
    }

    /// <summary>
    /// Gets the pixel ratio to be used.
    /// </summary>
    public float PixelRatio
    {
      get { return _pixelRatio; }
    }
  
    /// <summary>
    /// Calculates the aspect ratio for the <see cref="OriginalSize"/>.
    /// </summary>
    public float FrameAspectRatio
    {
      get { return _originalSize.Width/(float) _originalSize.Height; }
    }
  }
}