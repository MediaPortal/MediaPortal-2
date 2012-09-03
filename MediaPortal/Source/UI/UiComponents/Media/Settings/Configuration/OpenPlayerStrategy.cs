#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Services.Players.PCMOpenPlayerStrategy;
using MediaPortal.UI.Services.Players.Settings;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class OpenPlayerStrategy : SingleSelectionList
  {
    protected IDictionary<IResourceString, Type> _piPOpenStrategyTypes = new Dictionary<IResourceString, Type>();

    protected void Clear()
    {
      Selected = -1;
      _items.Clear();
      _piPOpenStrategyTypes.Clear();
    }

    protected string Add(Type strategyType)
    {
      string localizationResource = string.Format(Consts.RES_CONCURRENT_PLAYER_OPEN_STRATEGY_TEMPLATE, strategyType.FullName);
      IResourceString res = LocalizationHelper.CreateResourceString(localizationResource);
      _items.Add(res);
      _piPOpenStrategyTypes.Add(res, strategyType);
      return strategyType.FullName;
    }

    public override void Load()
    {
      base.Load();
      Clear();
      IList<String> strategyTypeNames = new List<string> {Add(typeof(Default)), Add(typeof(PreservePiP))};
      PlayerContextManagerSettings settings = SettingsManager.Load<PlayerContextManagerSettings>();
      Selected = strategyTypeNames.IndexOf(settings.OpenPlayerStrategyTypeName);
    }

    public override void Save()
    {
      base.Save();
      PlayerContextManagerSettings settings = SettingsManager.Load<PlayerContextManagerSettings>();
      int selected = Selected;
      IResourceString selectedItem = selected == -1 ? null : _items[selected];
      if (selectedItem != null)
      {
        settings.OpenPlayerStrategyTypeName = _piPOpenStrategyTypes[selectedItem].FullName;
        SettingsManager.Save(settings);
      }
    }
  }
}