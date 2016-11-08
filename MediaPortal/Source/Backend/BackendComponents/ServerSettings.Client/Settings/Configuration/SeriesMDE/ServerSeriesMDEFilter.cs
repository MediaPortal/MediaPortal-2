#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerSeriesMDEFilter : MultipleSelectionList, IDisposable
  {
    public ServerSeriesMDEFilter()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.Actors]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.Characters]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.Directors]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.Writers]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.ProductionStudios]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.SeriesMDESettings.ServerSeriesMDEFilter.TvNetwork]"));
    }

    public override void Load()
    {
      if (!Enabled)
        return;
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      if (settings.IncludeActorDetails)
        _selected.Add(0);
      if (settings.IncludeCharacterDetails)
        _selected.Add(1);
      if (settings.IncludeDirectorDetails)
        _selected.Add(2);
      if (settings.IncludeWriterDetails)
        _selected.Add(3);
      if (settings.IncludeProductionCompanyDetails)
        _selected.Add(4);
      if (settings.IncludeTVNetworkDetails)
        _selected.Add(5);
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      SeriesMetadataExtractorSettings settings = serverSettings.Load<SeriesMetadataExtractorSettings>();
      settings.IncludeActorDetails = _selected.Contains(0);
      settings.IncludeCharacterDetails = _selected.Contains(1);
      settings.IncludeDirectorDetails = _selected.Contains(2);
      settings.IncludeWriterDetails = _selected.Contains(3);
      settings.IncludeProductionCompanyDetails = _selected.Contains(4);
      settings.IncludeTVNetworkDetails = _selected.Contains(5);
      serverSettings.Save(settings);
      localSettings.Save(settings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
