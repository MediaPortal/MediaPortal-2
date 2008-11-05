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
using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Presentation.Geometry;

namespace MediaPortal.SkinEngine.Players.Geometry
{
  public class GeometryStretch : IGeometry
  {
    private StringId _name = new StringId("playback", "11");

    #region IGeometry Members

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


      rSource = new Rectangle(cropSettings.Left, cropSettings.Top, croppedImageWidth, croppedImageHeight);
      rDest = new Rectangle(0, 0, gemometry.ScreenWidth, gemometry.ScreenHeight);
    }

    #endregion
  }
}
