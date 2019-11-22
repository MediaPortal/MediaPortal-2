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

using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class SubtitleMatcher<TId> : BaseSubtitleMatcher<TId>, ISubtitleMatcher
  {   
    protected readonly SemaphoreSlim _initSyncObj = new SemaphoreSlim(1, 1);
    protected bool _isInit = false;

    #region Init

    public SubtitleMatcher(string name)
    {
      _id = GetType().Name;
      _name = name;
    }

    public override async Task<bool> InitAsync()
    {
      if (!_enabled)
        return false;

      await _initSyncObj.WaitAsync().ConfigureAwait(false);
      try
      {
        if (_isInit)
          return true;

        if (await InitWrapperAsync(UseSecureWebCommunication).ConfigureAwait(false))
        {
          _isInit = true;
          return true;
        }
        return false;
      }
      finally
      {
        _initSyncObj.Release();
      }
    }

    public abstract Task<bool> InitWrapperAsync(bool useHttps);

    public override bool Equals(object obj)
    {
      if (obj is SubtitleMatcher<TId> m)
        return Id.Equals(m.Id);
      return false;
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public override string ToString()
    {
      return Name;
    }

    #endregion

    #region Fields

    private bool _enabled = true;
    private string _preferredLanguageCulture = "en-US";
    private bool _useMediaAudioIfUnmatched = false;

    #endregion

    #region Properties

    public bool Enabled
    {
      get { return _enabled; }
      set { _enabled = value; }
    }

    public string PreferredLanguageCulture
    {
      get { return _preferredLanguageCulture; }
      set { _preferredLanguageCulture = value; }
    }

    public bool UseMediaAudioIfUnmatched
    {
      get { return _useMediaAudioIfUnmatched; }
      set { _useMediaAudioIfUnmatched = value; }
    }

    #endregion

    #region Subtitle search

    public virtual async Task<IEnumerable<SubtitleInfo>> FindMatchingMovieSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languageCultures)
    {
      List<SubtitleInfo> matches = new List<SubtitleInfo>();

      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return matches;

        SubtitleInfo subtitleSearch = subtitleInfo.Clone();
        if (languageCultures?.Any() ?? false)
          languageCultures = new List<string> { PreferredLanguageCulture };

        IEnumerable<SubtitleInfo> onlineMatches = null;
        Logger.Debug(_id + ": Search for subtitles for {0} online", subtitleInfo.MediaTitle);
        onlineMatches = await _wrapper.SearchMovieSubtitleMatchesAsync(subtitleSearch, languageCultures).ConfigureAwait(false);
        if (onlineMatches?.Count() > 0)
        {
          matches.AddRange(onlineMatches.Where(m => m.IsBaseInfoPresent));
        }

        return matches;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while finding matching subtitles for {0}", ex, subtitleInfo.MediaTitle);
        return matches;
      }
    }

    public virtual async Task<IEnumerable<SubtitleInfo>> FindMatchingEpisodeSubtitlesAsync(SubtitleInfo subtitleInfo, List<string> languageCultures)
    {
      List<SubtitleInfo> matches = new List<SubtitleInfo>();

      try
      {
        // Try online lookup
        if (!await InitAsync().ConfigureAwait(false))
          return matches;

        SubtitleInfo subtitleSearch = subtitleInfo.Clone();
        if (languageCultures?.Any() ?? false)
          languageCultures = new List<string> { PreferredLanguageCulture };

        IEnumerable<SubtitleInfo> onlineMatches = null;
        Logger.Debug(_id + ": Search for subtitles for {0} online", subtitleInfo.MediaTitle);
        onlineMatches = await _wrapper.SearchSeriesEpisodeSubtitleMatchesAsync(subtitleSearch, languageCultures).ConfigureAwait(false);
        if (onlineMatches?.Count() > 0)
        {
          matches.AddRange(onlineMatches.Where(m => m.IsBaseInfoPresent));
        }

        return matches;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + ": Exception while finding matching subtitles for {0}", ex, subtitleInfo.MediaTitle);
        return matches;
      }
    }

    public virtual async Task<bool> DownloadSubtitleAsync(SubtitleInfo info, bool overwriteExsting)
    {
      if (await InitAsync().ConfigureAwait(false))
        return await _wrapper.DownloadSubtitleMatchesAsync(info, overwriteExsting);

      return false;
    }

    #endregion
  }
}
