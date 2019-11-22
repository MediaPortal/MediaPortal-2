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
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data
{
  public class ScriptableScraperMovieMatcher : MovieMatcher<string, string>
  {
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromMinutes(10);

    #region Init

    public ScriptableScraperMovieMatcher(ScriptableScript script) : 
      base(script.Name, ServiceRegistration.Get<IPathManager>().GetPath($@"<DATA>\ScriptableScraperProvider\{script.ScriptID}"), MAX_MEMCACHE_DURATION, false)
    {
      _id = script.ScriptID.ToString();
      _wrapper = new ScriptableScraperMovieWrapper(script);

      PreferredLanguageCulture = script.Language;

      //Will be overridden if the user enables it in settings
      Enabled = false;
    }

    public override Task<bool> InitWrapperAsync(bool useHttps)
    {
      try
      {
        if (_wrapper is ScriptableScraperMovieWrapper wrapper)
          return Task.FromResult(wrapper.Init());

        return Task.FromResult(false);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ScriptableScraperMovieMatcher ({0}): Error initializing wrapper", ex, _name);
      }
      return Task.FromResult(false);
    }

    #endregion

    #region Translators

    protected override bool GetMovieId(MovieInfo movie, out string id)
    {
      id = null;
      if (movie.CustomIds.ContainsKey(_name))
        id = movie.CustomIds[_name];
      return id != null;
    }

    protected override bool SetMovieId(MovieInfo movie, string id)
    {
      if (!string.IsNullOrEmpty(id))
      {
        movie.CustomIds[_name] = id;
        return true;
      }
      return false;
    }

    #endregion

    public override string ToString()
    {
      return Name;
    }
  }
}
