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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor;
using MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerSeriesMDEOnlineData : SingleSelectionList, IDisposable
  {
    public ServerSeriesMDEOnlineData()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MDESettings.MDEOnlineData.MediaFanArt]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MDESettings.MDEOnlineData.Media]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MDESettings.MDEOnlineData.FanArt]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MDESettings.MDEOnlineData.None]"));
    }

    public override void Load()
    {
      if (!Enabled)
        return;
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      if (!settings.SkipOnlineSearches && !settings.SkipFanArtDownload)
        Selected = 0;
      else if (!settings.SkipOnlineSearches)
        Selected = 1;
      else if (!settings.SkipFanArtDownload)
        Selected = 2;
      else
        Selected = 3;
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();

      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      if (Selected == 0)
      {
        settings.SkipOnlineSearches = false;
        settings.SkipFanArtDownload = false;
      }
      else if (Selected == 1)
      {
        settings.SkipOnlineSearches = false;
        settings.SkipFanArtDownload = true;
      }
      else if (Selected == 2)
      {
        settings.SkipOnlineSearches = true;
        settings.SkipFanArtDownload = false;
      }
      else
      {
        settings.SkipOnlineSearches = true;
        settings.SkipFanArtDownload = true;
      }
      serverSettings.Save(settings);
      localSettings.Save(settings);

      NfoSeriesMetadataExtractorSettings nfoSettings = serverSettings.Load<NfoSeriesMetadataExtractorSettings>();
      nfoSettings.SkipFanArtDownload = settings.SkipFanArtDownload;
      serverSettings.Save(nfoSettings);
      localSettings.Save(nfoSettings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
