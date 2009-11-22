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
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.UI.SkinEngine.Settings;

namespace MediaPortal.UI.SkinEngine.Geometry
{
  /// <summary>
  /// Class which can do transformations for video windows.
  /// currently it supports zoom, zoom 14:9, normal, stretch, original, letterbox 4:3 and panscan 4:3.
  /// </summary>
  public class GeometryManager : IGeometryManager
  {
    private readonly IDictionary<string, IGeometry> _availableGeometries = new Dictionary<string, IGeometry>();
    private IGeometry _defaultVideoGeometry;
    private CropSettings _cropSettings = new CropSettings();

    public GeometryManager()
    {
      Add(_defaultVideoGeometry = new GeometryNormal());
      Add(new GeometryOrignal());
      Add(new GeometryStretch());
      Add(new GeometryZoom());
      Add(new GeometryZoom149());
      Add(new GeometryLetterBox());
      Add(new GeometryPanAndScan());
      Add(new GeometryIntelligentZoom());
      PlayerSettings settings = ServiceScope.Get<ISettingsManager>().Load<PlayerSettings>();
      string defaultGeometry = settings.DefaultGeometry;
      foreach (IGeometry geometry in _availableGeometries.Values)
        if (geometry.Name == defaultGeometry)
          _defaultVideoGeometry = geometry;
    }

    public void Add(IGeometry geometry)
    {
      _availableGeometries.Add(geometry.Name, geometry);
    }

    public void Remove(string geometryName)
    {
      _availableGeometries.Remove(geometryName);
    }

    public IGeometry DefaultVideoGeometry 
    {
      get { return _defaultVideoGeometry; }
      set
      {
        bool changed = _defaultVideoGeometry != value;
        _defaultVideoGeometry = value;
        ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
        PlayerSettings settings = settingsManager.Load<PlayerSettings>();
        settings.DefaultGeometry = _defaultVideoGeometry == null ? null : _defaultVideoGeometry.Name;
        if (changed)
          PlayerGeometryMessaging.SendGeometryChangedMessage(PlayerGeometryMessaging.ALL_PLAYERS);
        settingsManager.Save(settings);
      }
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
      Transform(_defaultVideoGeometry, data, out rSource, out rDest);
    }

    public void Transform(IGeometry geometry, GeometryData data, out Rectangle rSource, out Rectangle rDest)
    {
      geometry.Transform(data, _cropSettings, out rSource, out rDest);
    }
  }
}
