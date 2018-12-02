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
using MediaPortal.Common.Certifications;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SimApiV1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class SimApiWrapper : ApiWrapper<string, string>
  {
    protected SimApiV1 _simApiHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps)
    {
      _simApiHandler = new SimApiV1(cachePath, useHttps);
      return true;
    }

    #region Search

    public override async Task<List<MovieInfo>> SearchMovieAsync(MovieInfo movieSearch, string language)
    {
      List<SimApiMovieSearchItem> foundMovies = await _simApiHandler.SearchMovieAsync(movieSearch.MovieName.Text, 
        movieSearch.ReleaseDate.HasValue ? movieSearch.ReleaseDate.Value.Year : 0).ConfigureAwait(false);
      if (foundMovies == null || foundMovies.Count == 0) return null;
      return foundMovies.Select(m => new MovieInfo()
      {
        ImdbId = m.ImdbID,
        MovieName = new SimpleTitle(m.Title, true),
        ReleaseDate = m.Year.HasValue ? new DateTime(m.Year.Value, 1, 1) : default(DateTime?),
      }).ToList();
    }

    public override async Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<SimApiPersonSearchItem> foundPersons = await _simApiHandler.SearchPersonAsync(personSearch.Name).ConfigureAwait(false);
      if (foundPersons == null) return null;
      return foundPersons.Select(p => new PersonInfo()
      {
        ImdbId = p.ImdbID,
        Name = p.Name,
      }).ToList();
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, string language, bool cacheOnly)
    {
      try
      {
        SimApiMovie movieDetail = null;
        if (!string.IsNullOrEmpty(movie.ImdbId))
          movieDetail = await _simApiHandler.GetMovieAsync(movie.ImdbId, cacheOnly).ConfigureAwait(false);
        if (movieDetail == null) return false;

        movie.ImdbId = movieDetail.ImdbID;
        movie.MovieName = new SimpleTitle(movieDetail.Title, true);
        movie.Summary = new SimpleTitle(movieDetail.Plot, true);

        CertificationMapping cert = null;
        if (CertificationMapper.TryFindMovieCertification(movieDetail.Rated, out cert))
        {
          movie.Certification = cert.CertificationId;
        }

        movie.Runtime = movieDetail.Duration.HasValue ? movieDetail.Duration.Value : 0;
        movie.ReleaseDate = movieDetail.Year.HasValue ? (DateTime?)new DateTime(movieDetail.Year.Value, 1, 1) : null;

        if (movieDetail.ImdbRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.ImdbRating, 1));
        }

        movie.Genres = movieDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();

        //Only use these if absolutely necessary because there is no way to ID them
        if (movie.Actors.Count == 0)
          movie.Actors = ConvertToPersons(movieDetail.Actors, PersonAspect.OCCUPATION_ACTOR, movieDetail.Title);
        if (movie.Writers.Count == 0)
          movie.Writers = ConvertToPersons(movieDetail.Writers, PersonAspect.OCCUPATION_WRITER, movieDetail.Title);
        //if (movie.Directors.Count == 0)
        //  movie.Directors = ConvertToPersons(movieDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR, movieDetail.Title);

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SimApiWrapper: Exception while processing movie {0}", ex, movie.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMoviePersonAsync(MovieInfo movieInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        SimApiPerson personDetail = null;
        if (!string.IsNullOrEmpty(person.ImdbId))
          personDetail = await _simApiHandler.GetPersonAsync(person.ImdbId, cacheOnly).ConfigureAwait(false);
        if (personDetail == null) return false;

        person.ImdbId = personDetail.ImdbID;
        person.Name = personDetail.Name;
        person.DateOfBirth = personDetail.BirthYear.HasValue ? (DateTime?)new DateTime(personDetail.BirthYear.Value, 1, 1) : null;
        person.Orign = personDetail.BirthPlace;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(List<string> names, string occupation, string media, string parentMedia = null)
    {
      if (names == null || names.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string name in names)
        retValue.Add(new PersonInfo() { Name = name, Occupation = occupation, Order = sortOrder++, MediaName = media, ParentMediaName = parentMedia });
      return retValue;
    }

    #endregion

    #region FanArt

    public override Task<ApiWrapperImageCollection<string>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (fanartMediaType == FanArtMediaTypes.Movie)
        return GetMovieFanArtAsync(infoObject as MovieInfo);
      if (fanartMediaType == FanArtMediaTypes.Actor || fanartMediaType == FanArtMediaTypes.Director || fanartMediaType == FanArtMediaTypes.Writer)
        return GetPersonFanArtAsync(infoObject as PersonInfo);
      return Task.FromResult<ApiWrapperImageCollection<string>>(null);
    }

    public override Task<bool> DownloadFanArtAsync(string id, string image, string folderPath)
    {
      return _simApiHandler.DownloadImageAsync(id, image, folderPath);
    }

    protected async Task<ApiWrapperImageCollection<string>> GetMovieFanArtAsync(MovieInfo movie)
    {
      if (movie == null || string.IsNullOrEmpty(movie.ImdbId))
        return null;
      SimApiMovie movieDetail = await _simApiHandler.GetMovieAsync(movie.ImdbId, false).ConfigureAwait(false);
      if (movieDetail == null || string.IsNullOrEmpty(movieDetail.PosterUrl))
        return null;
      ApiWrapperImageCollection<string> images = new ApiWrapperImageCollection<string>();
      images.Id = movie.ImdbId;
      images.Posters.Add(movieDetail.PosterUrl);
      return images;
    }

    protected async Task<ApiWrapperImageCollection<string>> GetPersonFanArtAsync(PersonInfo person)
    {
      if (person == null || string.IsNullOrEmpty(person.ImdbId))
        return null;
      SimApiPerson personDetail = await _simApiHandler.GetPersonAsync(person.ImdbId, false).ConfigureAwait(false);
      if (personDetail == null || string.IsNullOrEmpty(personDetail.ImageUrl))
        return null;
      ApiWrapperImageCollection<string> images = new ApiWrapperImageCollection<string>();
      images.Id = person.ImdbId;
      images.Posters.Add(personDetail.ImageUrl);
      return images;
    }

    #endregion
  }
}
