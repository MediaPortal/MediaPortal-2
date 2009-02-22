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
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Geometry;

namespace Ui.Players.VideoPlayer.Geometry
{
  public class GeometryLetterBox : IGeometry
  {
    private StringId _name = new StringId("playback", "7");

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
    /// <param name="gemometry">The geometry helper.</param>
    /// <param name="fSourceFrameRatio">The source aspect ratio.</param>
    /// <param name="rSource">The source rectangle.</param>
    /// <param name="rDest">The destination rectangle.</param>
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


      // shrink movie 33% vertically
      float fNewWidth = (float) gemometry.ScreenWidth;
      float fNewHeight = (float) (fNewWidth/fOutputFrameRatio);
      fNewHeight *= (1.0f - 0.33333333333f);

      if (fNewHeight > gemometry.ScreenHeight)
      {
        fNewHeight = gemometry.ScreenHeight;
        fNewHeight *= (1.0f - 0.33333333333f);
        fNewWidth = fNewHeight*fOutputFrameRatio;
      }

      // this shouldnt happen, but just make sure that everything still fits onscreen
      if (fNewWidth > gemometry.ScreenWidth || fNewHeight > gemometry.ScreenHeight)
      {
        fNewWidth = (float) gemometry.ImageWidth;
        fNewHeight = (float) gemometry.ImageHeight;
      }

      // Centre the movie
      float iPosY = (gemometry.ScreenHeight - fNewHeight)/2;
      float iPosX = (gemometry.ScreenWidth - fNewWidth)/2;

      rSource = new Rectangle(0, 0, gemometry.ImageWidth, gemometry.ImageHeight);
      rDest = new Rectangle((int) iPosX, (int) iPosY, (int) (fNewWidth + 0.5f), (int) (fNewHeight + 0.5f));
      gemometry.AdjustSourceForCropping(ref rSource, cropSettings);
    }
  }
}
