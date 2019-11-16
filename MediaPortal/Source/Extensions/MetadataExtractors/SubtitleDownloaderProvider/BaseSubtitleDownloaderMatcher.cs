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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.SubtitleDownloaderProvider
{
  public class BaseSubtitleDownloaderMatcher : SubtitleMatcher<string>
  {
    private string _subtitleDownloaderProviderId = "";
    private string _providerName = "";

    #region Init

    public BaseSubtitleDownloaderMatcher(string id, string name, string subtitleDownloaderProvider, string providerName) : base(id, name)
    {
      _subtitleDownloaderProviderId = subtitleDownloaderProvider;
      _providerName = providerName;

      //Will be overridden if the user enables it in setttings
      Enabled = subtitleDownloaderProvider == "OpenSubtitles" || subtitleDownloaderProvider == "MovieSubtitles" || subtitleDownloaderProvider == "Podnapisi" || subtitleDownloaderProvider == "Subscene" || subtitleDownloaderProvider == "TvSubtitles";
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        var providerNames = SubtitleDownloader.SubtitleDownloaderSetup.GetSupportedProviderNames();
        if (!providerNames.Contains(_subtitleDownloaderProviderId))
        {
          Enabled = false;
          ServiceRegistration.Get<ILogger>().Error("SubtitleDownloaderMatcher ({0}): Unknown wrapper", _subtitleDownloaderProviderId);
          return Task.FromResult(false);
        }

        SubtitleDownloaderWrapper wrapper = new SubtitleDownloaderWrapper();
        if (wrapper.Init(_subtitleDownloaderProviderId, _providerName))
        {
          _wrapper = wrapper;
          return Task.FromResult(true);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SubtitleDownloaderMatcher ({0}): Error initializing wrapper", ex, _subtitleDownloaderProviderId);
      }
      return Task.FromResult(false);
    }

    #endregion
  }
}
