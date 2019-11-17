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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider.Implementations;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider
{
  public class SubtitleDownloaderProviderPlugin : IPluginStateTracker
  {
    public SubtitleDownloaderProviderPlugin()
    {
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      Logger.Debug("SubtitleDownloaderProviderPlugin: Registering matchers");
      var providerNames = SubtitleDownloader.SubtitleDownloaderSetup.GetSupportedProviderNames();
      var providers = new BaseSubtitleDownloaderMatcher[]
      {
        new MovieSubtitlesMatcher(),
        new OpenSubtitlesMatcher(),
        new PodnapisiMatcher(),
        new SousTitresMatcher(),
        new SublightMatcher(),
        new SubsceneMatcher(),
        new TitloviMatcher(),
        new TitulkyMatcher(),
        new TvSubtitlesMatcher(),
      };
      var onlineService = ServiceRegistration.Get<IOnlineMatcherService>();
      onlineService.RegisterSubtitleMatchers(providers.Where(p => providerNames.Contains(p.SubtitleDownloaderProviderId)).ToArray());
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
