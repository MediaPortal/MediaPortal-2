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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data
{
  public class ScriptableScraperMovieWrapper : ApiMediaWrapper<string, string>
  {
    protected readonly ScriptableScript _script;
    protected readonly string _name;
    protected readonly string _language;
    private readonly Downloader _downloader;

    public ScriptableScraperMovieWrapper(ScriptableScript script)
    {
      _name = script.Name;
      _language = script.LanguageCode;
      _script = script;
      _downloader = new Downloader { EnableCompression = true };
    }

    public bool Init()
    {
      return _script.Scraper != null;
    }

    #region Search

    public override async Task<List<MovieInfo>> SearchMovieAsync(MovieInfo movieSearch, string language)
    {
      if (_language != null && !_language.Equals(language, StringComparison.InvariantCultureIgnoreCase))
        return null;

      await Task.Yield();
      List<MovieInfo> foundMovies = _script.SearchMovie(movieSearch);
      if (foundMovies == null || foundMovies.Count == 0) return null;
      return foundMovies;
    }

    #endregion

    #region Update

    public override Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;
        if (_language != null && !_language.Equals(language, StringComparison.InvariantCultureIgnoreCase))
          return Task.FromResult(false);
        if (cacheOnly)
          return Task.FromResult(false);

        if (!_script.UpdateMovie(movie))
          return Task.FromResult(false);

        if (!movie.DataProviders.Contains(_name))
          movie.DataProviders.Add(_name);

        return Task.FromResult(true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(_name + ": Exception in while processing movie {0}", ex, movie.ToString());
        return Task.FromResult(false);
      }
    }

    #endregion

    #region FanArt

    public override async Task<ApiWrapperImageCollection<string>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (_language != null && !_language.Equals(language, StringComparison.InvariantCultureIgnoreCase))
        return null;

      await Task.Yield();
      MovieInfo info = infoObject as MovieInfo;
      if (fanartMediaType == FanArtMediaTypes.Movie && info != null && info.CustomIds.ContainsKey(_name))
      {
        ApiWrapperImageCollection<string> images = new ApiWrapperImageCollection<string>();
        images.Id = info.CustomIds[_name];

        var posters = _script.GetArtwork(info);
        if (posters?.Count > 0)
          images.Posters.AddRange(posters);

        var backdrops = _script.GetBackdrops(info);
        if (backdrops?.Count > 0)
          images.Backdrops.AddRange(backdrops);

        return images;
      }
      return null;
    }

    public override Task<bool> DownloadFanArtAsync(string id, string image, string folderPath)
    {
      string cacheFileName = CreateAndGetCacheName(id, image, folderPath);
      if (string.IsNullOrEmpty(cacheFileName))
        return Task.FromResult(false);

      var uri = new Uri(image);
      if (uri.Scheme == "file")
      {
        var data = File.ReadAllBytes(uri.LocalPath);
        File.WriteAllBytes(cacheFileName, data);
        return Task.FromResult(true);
      }
      else
      {
        return _downloader.DownloadFileAsync(image, cacheFileName);
      }
    }

    protected string CreateAndGetCacheName(string id, string imageUrl, string folderPath)
    {
      try
      {
        string prefix = string.Format(@"{0}({1})_", _script.ScriptID, id);
        if (!Directory.Exists(folderPath))
          Directory.CreateDirectory(folderPath);
        return Path.Combine(folderPath, prefix + imageUrl.Substring(imageUrl.LastIndexOf('/') + 1));
      }
      catch
      {
        // TODO: logging
        return null;
      }
    }

    #endregion
  }
}
