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
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace MediaPortal.Presentation.Geometry
{
  public interface IGeometryHelper
  {
    void Add(IGeometry geometry);
    void Remove(IGeometry geometry);
    void Select(string geometryName);
    List<IGeometry> Geometries { get;}
    int ImageWidth { get;}
    int ImageHeight { get;}

    int ScreenWidth { get;}
    int ScreenHeight { get;}
    float PixelRatio { get;}
    void AdjustSourceForCropping(ref Rectangle rSource, ICropSettings cropSettings);

    /// <summary>
    /// Gets the current geometry in use
    /// </summary>
    /// <value>The current geometry.</value>
    IGeometry Current { get;}
  }
}
