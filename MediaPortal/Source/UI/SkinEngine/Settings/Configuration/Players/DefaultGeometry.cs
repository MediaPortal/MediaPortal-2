#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Players
{
  public class DefaultGeometry : SingleSelectionList
  {
    #region Variables

    private IList<IGeometry> _geometries;

    #endregion

    #region Base overrides

    public override void Load()
    {
      IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
      _geometries = new List<IGeometry>(geometryManager.AvailableGeometries.Values);
      IGeometry current = geometryManager.DefaultVideoGeometry;
      // Fill items
      _items = new List<IResourceString>(_geometries.Count);
      for (int i = 0; i < _geometries.Count; i++)
      {
        IGeometry geometry = _geometries[i];
        _items.Add(LocalizationHelper.CreateResourceString(geometry.Name));
        if (geometry == current)
          Selected = i;
      }
    }

    public override void Save()
    {
      ServiceRegistration.Get<IGeometryManager>().DefaultVideoGeometry = _geometries[Selected];
    }

    #endregion
  }
}