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
using MediaPortal.Presentation.Geometries;

namespace MediaPortal.SkinEngine.Geometry
{
  public class GeometryPanAndScan : IGeometry
  {
    public const string NAME = "[Geometries.PanAndScan]";

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

      // assume that the movie is widescreen first, so use full height
      float fVertBorder = 0;
      float fNewHeight = data.TargetSize.Height;
      float fNewWidth = fNewHeight*fOutputFrameRatio*1.66666666667f;
      float fHorzBorder = (fNewWidth - data.TargetSize.Width)/2.0f;
      float fFactor = fNewWidth/data.OriginalSize.Width;
      fFactor *= data.PixelRatio;
      fHorzBorder = fHorzBorder/fFactor;

      if ((int) fNewWidth < data.TargetSize.Width)
      {
        fHorzBorder = 0;
        fNewWidth = data.TargetSize.Width;
        fNewHeight = fNewWidth/fOutputFrameRatio;
        fVertBorder = (fNewHeight - data.TargetSize.Height)/2.0f;
        fFactor = fNewWidth/data.OriginalSize.Width;
        fFactor *= data.PixelRatio;
        fVertBorder = fVertBorder/fFactor;
      }

      rSource = new Rectangle((int) fHorzBorder, (int) fVertBorder,
          (int) (data.OriginalSize.Width - 2.0f*fHorzBorder), (int) (data.OriginalSize.Height - 2.0f*fVertBorder));
      rDest = new Rectangle(0, 0, data.TargetSize.Width, data.TargetSize.Height);
      cropSettings.AdjustSource(ref rSource);
    }

    #endregion
  }
}
