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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Common.Localization;

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerSeriesMDEBrowseOffline : MultipleSelectionList, IDisposable
  {
    public ServerSeriesMDEBrowseOffline()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEBrowseOfflineNetwork]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEBrowseOfflineLocal]"));
    }

    public override void Load()
    {
      if (!Enabled)
        return;
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      if (settings.CacheOfflineFanArt)
        _selected.Add(0);
      if (settings.CacheLocalFanArt)
        _selected.Add(1);
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      settings.CacheOfflineFanArt = _selected.Contains(0);
      settings.CacheLocalFanArt = _selected.Contains(1);
      serverSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
