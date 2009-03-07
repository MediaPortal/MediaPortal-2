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
      float fOutputFrameRatio = data.FrameAspectRatio/data.PixelRatio;

      // make sure the crop settings are acceptable
      cropSettings = cropSettings.EnsureSanity(data.OriginalSize.Width, data.OriginalSize.Height);

      // maximize the movie width
      float fNewWidth = Math.Min(data.OriginalSize.Width, data.TargetSize.Width);
      float fNewHeight = fNewWidth/fOutputFrameRatio;

      if (fNewHeight > data.TargetSize.Height)
      {
        fNewHeight = Math.Min(data.OriginalSize.Height, data.TargetSize.Height);
        fNewWidth = fNewHeight*fOutputFrameRatio;
      }

      // this shouldnt happen, but just make sure that everything still fits onscreen
      if (fNewWidth > data.TargetSize.Width || fNewHeight > data.TargetSize.Height)
      {
        //Log.Error("Original Zoom Mode: 'this shouldnt happen' {0}x{1}", fNewWidth, fNewHeight);
        GeometryZoom zoom = new GeometryZoom();
        zoom.Transform(data, cropSettings, out rSource, out rDest);
        return;
      }

      // Centre the movie
      float iPosY = (data.TargetSize.Height - fNewHeight)/2;
      float iPosX = (data.TargetSize.Width - fNewWidth)/2;

      // The original zoom mode ignores cropping parameters:
      rSource = new Rectangle(0, 0, data.OriginalSize.Width, data.OriginalSize.Height);
      rDest = new Rectangle((int) iPosX, (int) iPosY, (int) (fNewWidth + 0.5f), (int) (fNewHeight + 0.5f));
    }

    #endregion
  }
}
