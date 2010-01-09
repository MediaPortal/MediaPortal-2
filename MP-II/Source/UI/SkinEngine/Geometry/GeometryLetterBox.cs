#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.Presentation.Geometries;

namespace MediaPortal.UI.SkinEngine.Geometry
{
  /// <summary>
  /// Cropping = Yes
  /// Stretch = UniformToFill
  /// Zoom = X:0; Y: 2/3
  /// Shader = None
  /// </summary>
  public class GeometryLetterBox : IGeometry
  {
    public const string NAME = "[Geometries.LetterBox]";

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
      float fOutputFrameRatio = data.FrameAspectRatio/data.PixelRatio;

      // make sure the crop settings are acceptable
      cropSettings = cropSettings.EnsureSanity(data.OriginalSize.Width, data.OriginalSize.Height);

      // shrink movie 33% vertically
      float fNewWidth = data.TargetSize.Width;
      float fNewHeight = fNewWidth/fOutputFrameRatio;
      fNewHeight *= (1.0f - 0.33333333333f);

      if (fNewHeight > data.TargetSize.Height)
      {
        fNewHeight = data.TargetSize.Height;
        fNewHeight *= (1.0f - 0.33333333333f);
        fNewWidth = fNewHeight*fOutputFrameRatio;
      }

      // Centre the movie
      float iPosY = (data.TargetSize.Height - fNewHeight)/2;
      float iPosX = (data.TargetSize.Width - fNewWidth)/2;

      rSource = new Rectangle(0, 0, data.OriginalSize.Width, data.OriginalSize.Height);
      rDest = new Rectangle((int) iPosX, (int) iPosY, (int) (fNewWidth + 0.5f), (int) (fNewHeight + 0.5f));
      cropSettings.AdjustSource(ref rSource);
    }

    #endregion
  }
}
