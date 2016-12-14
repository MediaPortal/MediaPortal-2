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
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;

namespace MediaPortal.Plugins.ServerSettings.Settings.Configuration
{
  public class ServerMovieMDEFilter : MultipleSelectionList, IDisposable
  {
    public ServerMovieMDEFilter()
    {
      Enabled = false;
      ConnectionMonitor.Instance.RegisterConfiguration(this);
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MovieMDESettings.ServerMovieMDEFilter.Actors]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MovieMDESettings.ServerMovieMDEFilter.Characters]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MovieMDESettings.ServerMovieMDEFilter.Directors]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MovieMDESettings.ServerMovieMDEFilter.Writers]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.ServerSettings.MovieMDESettings.ServerMovieMDEFilter.ProductionStudios]"));
    }

    public override void Load()
    {
      if (!Enabled)
        return;
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
      MovieMetadataExtractorSettings settings = serverSettings.Load<MovieMetadataExtractorSettings>();
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
    }

    public override void Save()
    {
      if (!Enabled)
        return;

      base.Save();

      ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
      IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();

      MovieMetadataExtractorSettings mainSettings = serverSettings.Load<MovieMetadataExtractorSettings>();
      mainSettings.IncludeActorDetails = _selected.Contains(0);
      mainSettings.IncludeCharacterDetails = _selected.Contains(1);
      mainSettings.IncludeDirectorDetails = _selected.Contains(2);
      mainSettings.IncludeWriterDetails = _selected.Contains(3);
      mainSettings.IncludeProductionCompanyDetails = _selected.Contains(4);
      serverSettings.Save(mainSettings);
      localSettings.Save(mainSettings);

      NfoMovieMetadataExtractorSettings nfoSettings = serverSettings.Load<NfoMovieMetadataExtractorSettings>();
      nfoSettings.IncludeActorDetails = mainSettings.IncludeActorDetails;
      nfoSettings.IncludeCharacterDetails = mainSettings.IncludeCharacterDetails;
      nfoSettings.IncludeDirectorDetails = mainSettings.IncludeDirectorDetails;
      serverSettings.Save(nfoSettings);
      localSettings.Save(nfoSettings);
    }

    public void Dispose()
    {
      ConnectionMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
