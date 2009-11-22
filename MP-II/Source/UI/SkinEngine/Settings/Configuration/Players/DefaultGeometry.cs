#region Copyright (C) 2007-2009 Team MediaPortal

/*
 *  Copyright (C) 2007-2009 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Configuration.ConfigurationClasses;

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
      IGeometryManager geometryManager = ServiceScope.Get<IGeometryManager>();
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
      ServiceScope.Get<IGeometryManager>().DefaultVideoGeometry = _geometries[Selected];
    }

    #endregion
  }
}