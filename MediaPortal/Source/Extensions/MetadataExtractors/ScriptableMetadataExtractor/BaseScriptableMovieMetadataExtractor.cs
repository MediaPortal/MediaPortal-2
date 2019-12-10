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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data;
using MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Settings;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor
{
  /// <summary>
  /// MediaPortal 2 base scriptable metadata extractor implementation.
  /// </summary>
  public abstract class BaseScriptableMovieMetadataExtractor : MovieMetadataExtractor.MovieMetadataExtractor
  {
    #region Constants

    private static ConcurrentBag<string> MetadataExtractorCustomCategories = null;

    #endregion

    #region Ctor

    static BaseScriptableMovieMetadataExtractor()
    {
      try
      {
        LoadScripts();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ScriptableMetadataExtractor: Error initializing scripts", ex);
      }
    }

    public BaseScriptableMovieMetadataExtractor(string id)
    {
      try
      {
        bool loaded = MetadataExtractorCustomCategories.TryTake(out _category);

        List<MediaCategory> mediaCategories = new List<MediaCategory>();
        if (loaded)
        {
          MediaCategory mediaCategory;
          IMediaAccessor mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
          if (!mediaAccessor.MediaCategories.TryGetValue(_category, out mediaCategory))
            mediaCategory = mediaAccessor.RegisterMediaCategory(_category, new List<MediaCategory> { DefaultMediaCategories.Video });
          mediaCategories.Add(mediaCategory);
        }

        _metadata = new MetadataExtractorMetadata(new Guid(id), $"Scriptable movie metadata extractor ({(loaded ? _category : "Disabled")})", MetadataExtractorPriority.External, true,
            mediaCategories, new MediaItemAspectMetadata[]
                {
                MediaAspect.Metadata,
                MovieAspect.Metadata
                });
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ScriptableMetadataExtractor: Error initializing metadata extractor", ex);
      }
    }

    private static void LoadScripts()
    {
      MetadataExtractorCustomCategories = new ConcurrentBag<string>();

      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<ScriptableMetadataExtractorSettings>();

      //Load latest version of scripts
      Assembly assembly = Assembly.GetExecutingAssembly();
      Dictionary<string, List<ScriptableScraperMovieMatcher>> matchers = new Dictionary<string, List<ScriptableScraperMovieMatcher>>();
      List<string> categories = new List<string>();
      Dictionary<int, ScriptableScript> scripts = new Dictionary<int, ScriptableScript>();
      foreach (var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(assembly.Location), "MovieScraperScripts\\"), "*.xml"))
      {
        var script = new ScriptableScript(settings.DefaultUserAgent);
        if (script.Load(file))
        {
          if (string.IsNullOrEmpty(script.Category))
            script.Category = MEDIA_CATEGORY_NAME_MOVIE;

          if (!scripts.ContainsKey(script.ScriptID))
            scripts.Add(script.ScriptID, script);
          else if (Version.TryParse(scripts[script.ScriptID].Version, out var curVer) && Version.TryParse(script.Version, out var newVer) && newVer > curVer)
            scripts[script.ScriptID] = script;
        }
      }

      //Categorize scripts
      foreach (var script in scripts.Values)
      {
        if (!matchers.ContainsKey(script.Category))
          matchers.Add(script.Category, new List<ScriptableScraperMovieMatcher>());
        matchers[script.Category].Add(new ScriptableScraperMovieMatcher(script));
      }

      //Register movie matchers
      foreach (var matcher in matchers)
      {
        if (!matcher.Key.Equals(MEDIA_CATEGORY_NAME_MOVIE, StringComparison.OrdinalIgnoreCase))
        {
          if (!categories.Contains(matcher.Key))
          {
            //Store custom MDE movie category
            categories.Add(matcher.Key);
            MetadataExtractorCustomCategories.Add(matcher.Key);
          }
        }
        OnlineMatcherService.Instance.RegisterMovieMatchers(matcher.Value.ToArray(), matcher.Key);
      }
    }

    #endregion
  }
}
