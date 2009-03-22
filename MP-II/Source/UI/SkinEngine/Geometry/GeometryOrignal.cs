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

using System;
using System.Drawing;
using MediaPortal.Presentation.Geometries;

namespace MediaPortal.SkinEngine.Geometry
{
  /// <summary>
  /// Cropping = No
  /// Stretch = None
  /// Zoom = None
  /// Shader = None
  /// Characteristics: Scale down, if bigger than original (UniformToFill)
  /// </summary>
  public class GeometryOrignal : IGeometry
  {
    public const string NAME = "[Geometries.Original]";

    #region IGeometry Members

    public string Name
    {
      get { return NAME; }
    }

    public string Shader
    {
      get { return null; }
    }

    public void Transform(GeometryData data, CropSettings cropSettings, out Rectangle rSource, out Rectangle rDest)
    {
      float outputFrameRatio = data.FrameAspectRatio/data.PixelRatio;

      // maximize the movie width
      int newWidth = Math.Min(data.OriginalSize.Width, data.TargetSize.Width);
      int newHeight = (int) (newWidth/outputFrameRatio);

      if (newHeight > data.TargetSize.Height)
      {
        newHeight = Math.Min(data.OriginalSize.Height, data.TargetSize.Height);
        newWidth = (int) (newHeight*outputFrameRatio);
      }

      // Centre the movie
      int iPosY = (int) ((data.TargetSize.Height - newHeight)/2.0f);
      int iPosX = (int) ((data.TargetSize.Width - newWidth)/2.0f);

      // The original zoom mode ignores cropping parameters:
      rSource = new Rectangle(0, 0, data.OriginalSize.Width, data.OriginalSize.Height);
      rDest = new Rectangle(iPosX, iPosY, newWidth, newHeight);
    }

    #endregion
  }
}
