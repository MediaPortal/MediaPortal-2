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

#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.Presentation.Geometry;

namespace MediaPortal.SkinEngine.Players.Geometry
{
  /// <summary>
  /// Class which can do transformations for video windows
  /// currently it supports Zoom, Zoom 14:9, normal, stretch, original, letterbox 4:3 and panscan 4:3
  /// </summary>
  public class Geometry : IGeometryHelper
  {
    private int _imageWidth = 100; // width of the video window or image
    private int _imageHeight = 100; // height of the height window or image
    private int _screenWidth = 100; // width of the screen
    private int _screenHeight = 100; // height of the screen
    private float _pixelRatio = 1.0f; // pixelratio correction 
    private List<IGeometry> _geometries = new List<IGeometry>();
    private int _currentIndex = 0;

    /// <summary>
    /// Empty constructor
    /// </summary>
    public Geometry()
    {
      ServiceScope.Add<IGeometryHelper>(this);
      _geometries.Add(new GeometryNormal());
      _geometries.Add(new GeometryOrignal());
      _geometries.Add(new GeometryStretch());
      _geometries.Add(new GeometryZoom());
      _geometries.Add(new GeometryZoom149());
      _geometries.Add(new GeometryLetterBox());
      _geometries.Add(new GeometryPanAndScan());
      _geometries.Add(new GeometryIntelligentZoom());
    }

    /// <summary>
    /// Adds the specified geometry.
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    public void Add(IGeometry geometry)
    {
      _geometries.Add(geometry);
    }

    /// <summary>
    /// Removes the specified geometry.
    /// </summary>
    /// <param name="geometry">The geometry.</param>
    public void Remove(IGeometry geometry)
    {
      _geometries.Remove(geometry);
    }

    /// <summary>
    /// Selects the specified geometry .
    /// </summary>
    /// <param name="geometryName">Name of the geometry.</param>
    public void Select(string geometryName)
    {
      for (int i = 0; i < _geometries.Count; ++i)
      {
        if (geometryName == _geometries[i].Name)
        {
          Index = i;
          break;
        }
      }
    }

    /// <summary>
    /// Gets the current geometry in use
    /// </summary>
    /// <value>The current geometry.</value>
    public IGeometry Current 
    {
      get
      {
        return _geometries[Index];
      }
    }
    /// <summary>
    /// Gets the geometries.
    /// </summary>
    /// <value>The geometries.</value>
    public List<IGeometry> Geometries
    {
      get { return _geometries; }
    }

    /// <summary>
    /// Gets or sets the current geometry used.
    /// </summary>
    /// <value>The index.</value>
    public int Index
    {
      get { return _currentIndex; }
      set { _currentIndex = value; }
    }

    /// <summary>
    /// property to get/set the width of the video/image
    /// </summary>
    public int ImageWidth
    {
      get { return _imageWidth; }
      set { _imageWidth = value; }
    }

    /// <summary>
    /// property to get/set the height of the video/image
    /// </summary>
    public int ImageHeight
    {
      get { return _imageHeight; }
      set { _imageHeight = value; }
    }

    /// <summary>
    /// property to get/set the width of the screen
    /// </summary>
    public int ScreenWidth
    {
      get { return _screenWidth; }
      set { _screenWidth = value; }
    }

    /// <summary>
    /// property to get/set the height of the screen
    /// </summary>
    public int ScreenHeight
    {
      get { return _screenHeight; }
      set { _screenHeight = value; }
    }


    /// <summary>
    /// property to get/set the pixel ratio 
    /// </summary>
    public float PixelRatio
    {
      get { return _pixelRatio; }
      set { _pixelRatio = value; }
    }

    /// <summary>
    /// Method todo the transformation.
    /// It will calculate 2 rectangles. A source and destination rectangle based on the
    /// current transformation , image width/height and screen width/height
    /// the returned source rectangle specifies which part of the image/video should be copied
    /// the returned destination rectangle specifies where the copied part should be presented on screen
    /// </summary>
    /// <param name="rSource">rectangle containing the source rectangle of the image/video</param>
    /// <param name="rDest">rectangle  containing the destination rectangle of the image/video</param>
    public void GetWindow(out Rectangle rSource, out Rectangle rDest)
    {
      float fSourceFrameRatio = CalculateFrameAspectRatio();
      CropSettings cropSettings = new CropSettings();
      GetWindow(fSourceFrameRatio, out rSource, out rDest, cropSettings);
    }

    public void GetWindow(int arVideoWidth, int arVideoHeight, out Rectangle rSource, out Rectangle rDest)
    {
      CropSettings cropSettings = new CropSettings();
      GetWindow(arVideoWidth, arVideoHeight, out rSource, out rDest, cropSettings);
    }

    // used from planescene
    public void GetWindow(int arVideoWidth, int arVideoHeight, out Rectangle rSource, out Rectangle rDest, CropSettings cropSettings)
    {
      float fSourceFrameRatio = (float)arVideoWidth / (float)arVideoHeight;
      GetWindow(fSourceFrameRatio, out rSource, out rDest, cropSettings);
    }

    public void GetWindow(float fSourceFrameRatio, out Rectangle rSource, out Rectangle rDest, CropSettings cropSettings)
    {
      _geometries[_currentIndex].GetWindow(this, fSourceFrameRatio, out rSource, out rDest, cropSettings);
    }


    public void AdjustSourceForCropping(ref Rectangle rSource, ICropSettings cropSettings)
    {
      rSource.Y += cropSettings.Top;
      rSource.Height -= cropSettings.Top;
      rSource.Height -= cropSettings.Bottom;

      rSource.X += cropSettings.Left;
      rSource.Width -= cropSettings.Left;
      rSource.Width -= cropSettings.Right;
    }

    /// <summary>
    /// Calculates the aspect ratio for the current image/video window
    /// <returns>float value containing the aspect ratio
    /// </returns>
    /// </summary>
    private float CalculateFrameAspectRatio()
    {
      return (float)ImageWidth / (float)ImageHeight;
    }
  }
}
