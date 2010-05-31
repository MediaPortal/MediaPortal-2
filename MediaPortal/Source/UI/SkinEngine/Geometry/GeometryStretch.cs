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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.UI.Presentation.Geometries;

namespace MediaPortal.UI.SkinEngine.Geometry
{
  /// <summary>
  /// Cropping = Yes
  /// Stretch = Fill
  /// Zoom = None
  /// Shader = None
  /// </summary>
  public class GeometryStretch : IGeometry
  {
    public const string NAME = "[Geometries.Stretch]";

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
      // make sure the crop settings are acceptable
      cropSettings = cropSettings.EnsureSanity(data.OriginalSize.Width, data.OriginalSize.Height);

      int cropW = cropSettings.Left + cropSettings.Right;
      int cropH = cropSettings.Top + cropSettings.Bottom;

      // the source image dimensions when taking into
      // account the crop settings
      int croppedImageWidth = data.OriginalSize.Width - cropW;
      int croppedImageHeight = data.OriginalSize.Height - cropH;

      rSource = new Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
      rDest = new Rectangle(0, 0, data.TargetSize.Width, data.TargetSize.Height);
    }

    #endregion
  }
}
