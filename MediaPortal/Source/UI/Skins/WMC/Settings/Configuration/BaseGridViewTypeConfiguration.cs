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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using SkinSettings;
using System;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.WMCSkin.Settings.Configuration
{
  public abstract class BaseGridViewTypeConfiguration : SingleSelectionList, IDisposable
  {
    protected IList<GridViewType> _viewTypes;

    public BaseGridViewTypeConfiguration()
    {
      SkinChangeMonitor.Instance.RegisterConfiguration(WMCSkinSettings.SKIN_NAME, this);

      _viewTypes = new List<GridViewType>
      {
        GridViewType.Poster,
        GridViewType.Banner,
        GridViewType.Thumbnail
      };

      foreach (GridViewType viewType in _viewTypes)
        _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.GridViewType." + Enum.GetName(typeof(GridViewType), viewType) + "]"));
    }

    protected GridViewType SelectedViewType
    {
      get { return _viewTypes[Selected]; }
      set { Selected = _viewTypes.IndexOf(value); }
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(WMCSkinSettings.SKIN_NAME, this);
    }
  }
}
