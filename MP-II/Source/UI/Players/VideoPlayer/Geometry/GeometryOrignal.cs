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
using MediaPortal.Core;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Geometry;

namespace MediaPortal.SkinEngine.Players.Geometry
{
  public class GeometryOrignal : IGeometry
  {
    private StringId _name = new StringId("playback", "9");

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name
    {
      get { return _name.ToString(); }
    }

    /// <summary>
    /// Gets the name of the shader to use
    /// </summary>
    /// <value>The shader.</value>
    public string Shader
    {
      get
      {
        return "";
      }
    }
    /// <summary>
    /// Gets the window.
    /// </summary>
    /// <param name="gemometry">The gemometry.</param>
    /// <param name="fSourceFrameRatio">The  source aspect ratio.</param>
    /// <param name="rSource">The source rect.</param>
    /// <param name="rDest">The destination rect.</param>
    /// <param name="cropSettings">The crop settings.</param>
    public void GetWindow(IGeometryHelper gemometry, float fSourceFrameRatio, out Rectangle rSource, out Rectangle rDest,ICropSettings cropSettings)
    {
      float fOutputFrameRatio = fSourceFrameRatio/gemometry.PixelRatio;

      // make sure the crop settings are acceptable
      cropSettings = cropSettings.EnsureSanity(gemometry.ImageWidth, gemometry.ImageHeight);

      int cropW = cropSettings.Left + cropSettings.Right;
      int cropH = cropSettings.Top + cropSettings.Bottom;

      // the source image dimensions when taking into
      // account the crop settings
      int croppedImageWidth = gemometry.ImageWidth - cropW;
      int croppedImageHeight = gemometry.ImageHeight - cropH;

      // suggested by ziphnor
      float fSourcePixelRatio = fSourceFrameRatio/((float) gemometry.ImageWidth/(float) gemometry.ImageHeight);
      float fCroppedOutputFrameRatio = fSourcePixelRatio*((float) croppedImageWidth/(float) croppedImageHeight)/
                                       gemometry.PixelRatio;


      // maximize the movie width
      float fNewWidth = (float) Math.Min(gemometry.ImageWidth, gemometry.ScreenWidth);
      float fNewHeight = (float) (fNewWidth/fOutputFrameRatio);

      if (fNewHeight > gemometry.ScreenHeight)
      {
        fNewHeight = Math.Min(gemometry.ImageHeight, gemometry.ScreenHeight);
        fNewWidth = fNewHeight*fOutputFrameRatio;
      }

      // this shouldnt happen, but just make sure that everything still fits onscreen
      if (fNewWidth > gemometry.ScreenWidth || fNewHeight > gemometry.ScreenHeight)
      {
        //Log.Error("Original Zoom Mode: 'this shouldnt happen' {0}x{1}", fNewWidth, fNewHeight);
        GeometryZoom zoom = new GeometryZoom();
        zoom.GetWindow(gemometry, fSourceFrameRatio, out rSource, out rDest, cropSettings);
        return;
      }

      // Centre the movie
      float iPosY = (gemometry.ScreenHeight - fNewHeight)/2;
      float iPosX = (gemometry.ScreenWidth - fNewWidth)/2;

      // The original zoom mode ignores cropping parameters:
      rSource = new Rectangle(0, 0, gemometry.ImageWidth, gemometry.ImageHeight);
      rDest = new Rectangle((int) iPosX, (int) iPosY, (int) (fNewWidth + 0.5f), (int) (fNewHeight + 0.5f));
    }
  }
}
