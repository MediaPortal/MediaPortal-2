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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Presentation.Geometries;

namespace MediaPortal.SkinEngine.Geometry
{
  /// <summary>
  /// Class which can do transformations for video windows.
  /// currently it supports Zoom, Zoom 14:9, normal, stretch, original, letterbox 4:3 and panscan 4:3.
  /// </summary>
  public class GeometryManager : IGeometryManager
  {
    private readonly IDictionary<string, IGeometry> _availableGeometries = new Dictionary<string, IGeometry>();
    private IGeometry _currentVideoGeometry;
    private CropSettings _cropSettings = new CropSettings();

    public GeometryManager()
    {
      Add(_currentVideoGeometry = new GeometryNormal());
      Add(new GeometryOrignal());
      Add(new GeometryStretch());
      Add(new GeometryZoom());
      Add(new GeometryZoom149());
      Add(new GeometryLetterBox());
      Add(new GeometryPanAndScan());
      Add(new GeometryIntelligentZoom());
    }

    public void Add(IGeometry geometry)
    {
      _availableGeometries.Add(geometry.Name, geometry);
    }

    public void Remove(string geometryName)
    {
      _availableGeometries.Remove(geometryName);
    }

    public IGeometry CurrentVideoGeometry 
    {
      get { return _currentVideoGeometry; }
      set { _currentVideoGeometry = value; }
    }

    public IDictionary<string, IGeometry> AvailableGeometries
    {
      get { return _availableGeometries; }
    }

    public CropSettings CropSettings
    {
      get { return _cropSettings; }
      set { _cropSettings = value ?? new CropSettings(); }
    }

    public void Transform(GeometryData data, out Rectangle rSource, out Rectangle rDest)
    {
      _currentVideoGeometry.Transform(data, _cropSettings, out rSource, out rDest);
    }
  }
}
