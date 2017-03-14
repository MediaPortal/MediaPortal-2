#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Plugins.SlimTv.Client.Settings.Configuration
{
  public class EpgVisibleChannelInfoSetting : MultipleSelectionList
  {
    public EpgVisibleChannelInfoSetting()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[SlimTvClient.EpgVisibleChannelInfo.ChannelName]"));
      _items.Add(LocalizationHelper.CreateResourceString("[SlimTvClient.EpgVisibleChannelInfo.ChannelNumber]"));
      _items.Add(LocalizationHelper.CreateResourceString("[SlimTvClient.EpgVisibleChannelInfo.ChannelLogo]"));
    }

    public override void Load()
    {
      base.Load();
      var settings = SettingsManager.Load<SlimTvClientSettings>();
      if (settings.EpgShowChannelNames)
        _selected.Add(0);
      if (settings.EpgShowChannelNumbers)
        _selected.Add(1);
      if (settings.EpgShowChannelLogos)
        _selected.Add(2);
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<SlimTvClientSettings>();
      settings.EpgShowChannelNames = _selected.Contains(0);
      settings.EpgShowChannelNumbers = _selected.Contains(1);
      settings.EpgShowChannelLogos = _selected.Contains(2);
      SettingsManager.Save(settings);
    }
  }
}
