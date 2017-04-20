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
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeriesInfo"/> contains information about a series. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeriesInfo : BaseInfo
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { SeriesAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Year" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_YEAR_INDEX = 1;
    /// <summary>
    /// Format string that holds series name including premiere year.
    /// </summary>
    public static string SERIES_FORMAT_STR = "{0} ({1})";
    /// <summary>
    /// Short format string that holds series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0}";
    /// <summary>
    /// Format string that holds series name, season and episode numbers of next episode.
    /// </summary>
    public static string NEXT_EPISODE_FORMAT_STR = "{0} S{1:00}E{2:00}";

    protected static Regex _fromName = new Regex(@"(?<series>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public string ImdbId = null;
    public int TvMazeId = 0;
    public int TvRageId = 0;
    public string NameId = null;

    public SimpleTitle SeriesName = null;
    public SimpleTitle SeriesNameSort = new SimpleTitle();
    public string AlternateName = null;
    public string OriginalName = null;
    public int? SearchSeason = null;
    public int? SearchEpisode = null;
    /// <summary>
    /// Gets or sets the first aired date of series.
    /// </summary>
    public DateTime? FirstAired = null;
    public string Certification = null;
    public SimpleTitle Description = null;
    public bool IsEnded = false;

    public float Popularity = 0;
    public double Score = 0;
    public SimpleRating Rating = new SimpleRating();

    public SimpleTitle NextEpisodeName = null;
    public int? NextEpisodeSeasonNumber = null;
    public int? NextEpisodeNumber = null;
    public DateTime? NextEpisodeAirDate = null;
    public int TotalSeasons = 0;
    public int TotalEpisodes = 0;

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages = new List<string>();
    public List<PersonInfo> Actors = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> Networks = new List<CompanyInfo>();
    public List<CompanyInfo> ProductionCompanies = new List<CompanyInfo>();
    public List<GenreInfo> Genres = new List<GenreInfo>();
    public List<string> Awards = new List<string>();
    public List<EpisodeInfo> Episodes = new List<EpisodeInfo>();
    public List<SeasonInfo> Seasons = new List<SeasonInfo>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (SeriesName.IsEmpty)
          return false;
        if (!FirstAired.HasValue)
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (TvdbId > 0)
          return true;
        if (MovieDbId > 0)
          return true;
        if (TvMazeId > 0)
          return true;
        if (TvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(ImdbId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!SeriesName.IsEmpty)
      {
        if (FirstAired.HasValue)
          NameId = SeriesName.Text + "(" + FirstAired.Value.Year + ")";
        else
          NameId = SeriesName.Text;
        NameId = GetNameId(NameId);
      }
    }

    public SeriesInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (SeriesName.IsEmpty) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      if (!SeriesNameSort.IsEmpty)
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, SeriesNameSort.Text);
      else
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(SeriesName.Text));
      //MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, true); //Is maintained by medialibrary and metadataextractors
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, SeriesName.Text);
      if (!string.IsNullOrEmpty(OriginalName)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, OriginalName);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, CleanString(Description.Text));
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, Certification);
      MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_ENDED, IsEnded);
      if (TotalSeasons > 0) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NUM_SEASONS, TotalSeasons);
      if (TotalEpisodes > 0) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NUM_EPISODES, TotalEpisodes);

      if (NextEpisodeAirDate.HasValue)
      {
        if (!NextEpisodeName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, NextEpisodeName.Text);
        else if (NextEpisodeNumber.HasValue && NextEpisodeSeasonNumber.HasValue)
          MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, string.Format(NEXT_EPISODE_FORMAT_STR, SeriesName, NextEpisodeSeasonNumber.Value, NextEpisodeNumber.Value));
        if (NextEpisodeSeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, NextEpisodeSeasonNumber.Value);
        if (NextEpisodeNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, NextEpisodeNumber.Value);
        MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, NextEpisodeAirDate.Value);
      }

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, TvRageId.ToString());
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, NameId);

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, Popularity);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_SCORE, Score);

      if (!Rating.IsEmpty)
      {
        MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_ACTORS, Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());

      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_AWARDS, Awards.Where(a => !string.IsNullOrEmpty(a)).ToList());

      if (Networks.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_NETWORKS, Networks.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList());
      if (ProductionCompanies.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, SeriesAspect.ATTR_COMPANIES, ProductionCompanies.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList());

      aspectData.Remove(GenreAspect.ASPECT_ID);
      foreach (GenreInfo genre in Genres.Distinct())
      {
        MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(aspectData, GenreAspect.Metadata);
        genreAspect.SetAttribute(GenreAspect.ATTR_ID, genre.Id);
        genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre.Name);
      }

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      GetMetadataChanged(aspectData);

      if (aspectData.ContainsKey(SeriesAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ORIG_SERIES_NAME, out OriginalName);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_CERTIFICATION, out Certification);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ENDED, out IsEnded);

        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_POPULARITY, out Popularity);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SCORE, out Score);

        double? rating;
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_TOTAL_RATING, out rating);
        int? voteCount;
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_RATING_COUNT, out voteCount);
        Rating = new SimpleRating(rating, voteCount);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, out tempString);
        SeriesNameSort = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_DESCRIPTION, out tempString);
        Description = new SimpleTitle(tempString, false);

        int? count;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NUM_SEASONS, out count))
          TotalSeasons = count.Value;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NUM_EPISODES, out count))
          TotalEpisodes = count.Value;

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out NameId);

        //Brownard 17.06.2016
        //The returned type of the collection differs on the server and client.
        //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
        //Use the non generic Ienumerable to allow for both types.
        IEnumerable collection;

        Actors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_ACTORS, out collection))
          Actors.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ACTOR, ParentMediaName = SeriesName.Text }));
        foreach (PersonInfo actor in Actors)
          actor.AssignNameId();

        Characters.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_CHARACTERS, out collection))
          Characters.AddRange(collection.Cast<string>().Select(s => new CharacterInfo { Name = s, ParentMediaName = SeriesName.Text }));
        foreach (CharacterInfo character in Characters)
          character.AssignNameId();

        Genres.Clear();
        IList<MultipleMediaItemAspect> genreAspects;
        if (MediaItemAspect.TryGetAspects(aspectData, GenreAspect.Metadata, out genreAspects))
        {
          foreach (MultipleMediaItemAspect genre in genreAspects)
          {
            Genres.Add(new GenreInfo
            {
              Id = genre.GetAttributeValue<int?>(GenreAspect.ATTR_ID),
              Name = genre.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)
            });
          }
        }

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Cast<string>().Select(s => s));

        Networks.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NETWORKS, out collection))
          Networks.AddRange(collection.Cast<string>().Select(s => new CompanyInfo { Name = s, Type = CompanyAspect.COMPANY_TV_NETWORK }));
        foreach (CompanyInfo network in Networks)
          network.AssignNameId();

        ProductionCompanies.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_COMPANIES, out collection))
          ProductionCompanies.AddRange(collection.Cast<string>().Select(s => new CompanyInfo { Name = s, Type = CompanyAspect.COMPANY_PRODUCTION }));
        foreach (CompanyInfo company in ProductionCompanies)
          company.AssignNameId();

        DateTime dateNextEpisode;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_AIR_DATE, out dateNextEpisode) && dateNextEpisode > DateTime.Now)
        {
          NextEpisodeAirDate = dateNextEpisode;
          MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE_NAME, out tempString);
          NextEpisodeName = new SimpleTitle(tempString, false);
          MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_SEASON, out NextEpisodeSeasonNumber);
          MediaItemAspect.TryGetAttribute(aspectData, SeriesAspect.ATTR_NEXT_EPISODE, out NextEpisodeNumber);
        }

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          HasThumbnail = true;

        return true;
      }
      else if (aspectData.ContainsKey(SeasonAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out NameId);

        return true;
      }
      else if (aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out NameId);

        if (aspectData.ContainsKey(VideoAudioStreamAspect.ASPECT_ID))
        {
          Languages.Clear();
          IList<MultipleMediaItemAspect> audioAspects;
          if (MediaItemAspect.TryGetAspects(aspectData, VideoAudioStreamAspect.Metadata, out audioAspects))
          {
            foreach (MultipleMediaItemAspect audioAspect in audioAspects)
            {
              string language = audioAspect.GetAttributeValue<string>(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
              if (!string.IsNullOrEmpty(language))
              {
                if (!Languages.Contains(language))
                  Languages.Add(language);
              }
            }
          }
        }

        return true;
      }
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out tempString);
        SeriesName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out NameId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          HasThumbnail = true;

        if (aspectData.ContainsKey(VideoAudioStreamAspect.ASPECT_ID))
        {
          Languages.Clear();
          IList<MultipleMediaItemAspect> audioAspects;
          if (MediaItemAspect.TryGetAspects(aspectData, VideoAudioStreamAspect.Metadata, out audioAspects))
          {
            foreach (MultipleMediaItemAspect audioAspect in audioAspects)
            {
              string language = audioAspect.GetAttributeValue<string>(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE);
              if (!string.IsNullOrEmpty(language))
              {
                if (!Languages.Contains(language))
                  Languages.Add(language);
              }
            }
          }
        }

        return true;
      }
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, SeriesName);
    }

    public override bool FromString(string name)
    {
      if (name.Contains("("))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          SeriesName = match.Groups["series"].Value;
          int year = Convert.ToInt32(match.Groups["year"].Value);
          if (year > 0)
            FirstAired = new DateTime(year, 1, 1);
          return true;
        }
        return false;
      }
      SeriesName = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is SeriesInfo)
      {
        SeriesInfo otherSeries = otherInstance as SeriesInfo;
        MovieDbId = otherSeries.MovieDbId;
        ImdbId = otherSeries.ImdbId;
        TvdbId = otherSeries.TvdbId;
        TvMazeId = otherSeries.TvMazeId;
        TvRageId = otherSeries.TvRageId;
        NameId = otherSeries.NameId;
        return true;
      }
      else if (otherInstance is SeasonInfo)
      {
        SeasonInfo seriesSeason = otherInstance as SeasonInfo;
        MovieDbId = seriesSeason.SeriesMovieDbId;
        ImdbId = seriesSeason.SeriesImdbId;
        TvdbId = seriesSeason.SeriesTvdbId;
        TvMazeId = seriesSeason.SeriesTvMazeId;
        TvRageId = seriesSeason.SeriesTvRageId;
        NameId = seriesSeason.SeriesNameId;
        SearchSeason = seriesSeason.SeasonNumber;
        return true;
      }
      else if (otherInstance is EpisodeInfo)
      {
        EpisodeInfo seriesEpisode = otherInstance as EpisodeInfo;
        MovieDbId = seriesEpisode.SeriesMovieDbId;
        ImdbId = seriesEpisode.SeriesImdbId;
        TvdbId = seriesEpisode.SeriesTvdbId;
        TvMazeId = seriesEpisode.SeriesTvMazeId;
        TvRageId = seriesEpisode.SeriesTvRageId;
        NameId = seriesEpisode.SeriesNameId;
        SearchSeason = seriesEpisode.SeasonNumber;
        if(seriesEpisode.EpisodeNumbers.Count > 0)
          SearchEpisode = seriesEpisode.EpisodeNumbers[0];
        return true;
      }
      return false;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      if(FirstAired.HasValue)
        return string.Format(SERIES_FORMAT_STR, SeriesName.IsEmpty ? "[Unnamed Series]" : SeriesName.Text, FirstAired.Value.Year);
      return SeriesName.IsEmpty ? "[Unnamed Series]" : SeriesName.Text;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Series]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      SeriesInfo other = obj as SeriesInfo;
      if (other == null) return false;

      if (TvdbId > 0 && other.TvdbId > 0)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0)
        return MovieDbId == other.MovieDbId;
      if (TvMazeId > 0 && other.TvMazeId > 0)
        return TvMazeId == other.TvMazeId;
      if (TvRageId > 0 && other.TvRageId > 0)
        return TvRageId == other.TvRageId;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId))
        return string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase);

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty &&
        MatchNames(SeriesName.Text, other.SeriesName.Text) && FirstAired.HasValue && other.FirstAired.HasValue &&
        FirstAired.Value == other.FirstAired.Value)
        return true;
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty &&
        MatchNames(SeriesName.Text, other.SeriesName.Text))
        return true;

      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(SeriesInfo))
      {
        SeriesInfo info = new SeriesInfo();
        info.CopyIdsFrom(this);
        info.SeriesName = SeriesName;
        info.SeriesNameSort = SeriesNameSort;
        info.FirstAired = FirstAired;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion
  }
}
