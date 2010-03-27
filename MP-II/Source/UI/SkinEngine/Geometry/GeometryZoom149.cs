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
  /// Stretch = UniformToFill
  /// Zoom = X:0;Y:1.125
  /// Shader = None
  /// </summary>
  public class GeometryZoom149 : IGeometry
  {
    public const string NAME = "[Geometries.Zoom149]";

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

      // fit the image to screen size
      float fNewWidth = data.TargetSize.Width;
      float fNewHeight = fNewWidth/fOutputFrameRatio;

      if (fNewHeight > data.TargetSize.Height)
      {
        fNewHeight = data.TargetSize.Height;
        fNewWidth = fNewHeight*fOutputFrameRatio;
      }

      float iPosX = 0;
      float iPosY = 0;
      float fVertBorder = 0;
      float fHorzBorder = 0;
      float fFactor = fNewWidth/data.OriginalSize.Width;
      fFactor *= data.PixelRatio;
      // increase the image size by 12.5% and crop or pad if needed
      fNewHeight = fNewHeight*1.125f;
      fNewWidth = fNewHeight*fOutputFrameRatio;

      if ((int) fNewHeight < data.TargetSize.Height)
      {
        fHorzBorder = (fNewWidth - data.TargetSize.Width)/2.0f;
        fHorzBorder = fHorzBorder/fFactor;
        iPosY = (data.TargetSize.Height - fNewHeight)/2;
      }

      if ((int) fNewWidth < data.TargetSize.Width)
      {
        fVertBorder = (fNewHeight - data.TargetSize.Height)/2.0f;
        fVertBorder = fVertBorder/fFactor;
        iPosX = (data.TargetSize.Width - fNewWidth)/2;
      }

      if ((int) fNewWidth > data.TargetSize.Width && (int) fNewHeight > data.TargetSize.Height)
      {
        fHorzBorder = (fNewWidth - data.TargetSize.Width)/2.0f;
        fHorzBorder = fHorzBorder/fFactor;
        fVertBorder = (fNewHeight - data.TargetSize.Height)/2.0f;
        fVertBorder = fVertBorder/fFactor;
      }

      rSource = new Rectangle((int) fHorzBorder, (int) fVertBorder,
          (int) (data.OriginalSize.Width - 2.0f*fHorzBorder), (int) (data.OriginalSize.Height - 2.0f*fVertBorder));
      rDest =
        new Rectangle((int) iPosX, (int) iPosY, (int) (fNewWidth - (2.0f*fHorzBorder*fFactor) + 0.5f),
                      (int) (fNewHeight - (2.0f*fVertBorder*fFactor) + 0.5f));
      cropSettings.AdjustSource(ref rSource);
    }

    #endregion
  }
}
