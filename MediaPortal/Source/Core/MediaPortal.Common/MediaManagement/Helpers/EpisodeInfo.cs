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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="EpisodeInfo"/> contains metadata information about a series episode item.
  /// </summary>
  /// <remarks>
  /// If all required fields are filled, the <see cref="AreReqiredFieldsFilled"/> 
  /// returns <c>true</c>. The <see cref="ToString"/> method returns a well formatted series title if <see cref="AreReqiredFieldsFilled"/> is <c>true</c>.
  /// </remarks>
  public class EpisodeInfo : BaseInfo
  {
    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Season" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SEASON_INDEX = 1;
    /// <summary>
    /// Returns the index for "Episode Number(s)" used in <see cref="FormatString"/>.
    /// </summary>
    public static int EPISODENUM_INDEX = 2;
    /// <summary>
    /// Returns the index for "Episode" used in <see cref="FormatString"/>.
    /// </summary>
    public static int EPISODE_INDEX = 3;
    /// <summary>
    /// Format string that holds series name, season and episode numbers and episode name.
    /// </summary>
    public static string EPISODE_FORMAT_STR = "{0} S{1}E{2} - {3}";
    /// <summary>
    /// Short format string that holds season and episode numbers and episode name. Used for browsing episodes by series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "S{1}E{2} - {3}";
    /// <summary>
    /// Format string for constructing a "Series Season" name pattern.
    /// </summary>
    public static string SERIES_SEASON_FORMAT_STR = "{0} S{1}";
    /// <summary>
    /// Format string that holds series name including premiere year.
    /// </summary>
    public static string SERIES_FORMAT_STR = "{0} ({1})";

    /// <summary>
    /// Used to replace all "." and "_" that are not followed by a word character.
    /// <example>Replaces <c>"Once.Upon.A.Time.S01E13"</c> to <c>"Once Upon A Time S01E13"</c>, but keeps the <c>"."</c> inside
    /// <c>"Dr. House"</c>.</example>
    /// </summary>
    protected static Regex _cleanUpWhiteSpaces = new Regex(@"[\.|_](\S|$)");
    protected static Regex _fromName = new Regex(@"(?<series>[^\s]*) S(?<season>\d{1,2})E(?<episode>\d{1,2}).* - (?<title>.*)", RegexOptions.IgnoreCase);
    protected static Regex _fromSeriesName = new Regex(@"(?<series>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Indicates that all required fields are filled.
    /// </summary>
    public bool AreReqiredFieldsFilled
    {
      get
      {
        return !(SeriesName.IsEmpty || !SeasonNumber.HasValue || EpisodeNumbers.Count == 0);
      }
    }

    /// <summary>
    /// Gets or sets the episode IMDB id.
    /// </summary>
    public string ImdbId = null;
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public int TvRageId = 0;

    /// <summary>
    /// Gets or sets the series IMDB id.
    /// </summary>
    public string SeriesImdbId = null;
    /// <summary>
    /// Gets or sets the series TheTvDB id.
    /// </summary>
    public int SeriesTvdbId = 0;
    public int SeriesMovieDbId = 0;
    public int SeriesTvMazeId = 0;
    public int SeriesTvRageId = 0;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public LanguageText SeriesName = null;
    public DateTime? SeriesFirstAired = null;
    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    public LanguageText EpisodeName = null;
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets a list of episode numbers.
    /// </summary>
    public List<int> EpisodeNumbers = new List<int>();
    /// <summary>
    /// Gets a list of episode numbers as they are released on DVD.
    /// </summary>
    public List<double> DvdEpisodeNumbers = new List<double>();
    /// <summary>
    /// Gets or sets the first aired date of episode.
    /// </summary>
    public DateTime? FirstAired = null;
    /// <summary>
    /// Gets or sets the episode summary.
    /// </summary>
    public LanguageText Summary = null;
    public double TotalRating = 0;
    public int RatingCount = 0;

    /// <summary>
    /// Gets a list of actors.
    /// </summary>
    public List<PersonInfo> Actors = new List<PersonInfo>();
    /// <summary>
    /// Gets a list of directors.
    /// </summary>
    public List<PersonInfo> Directors = new List<PersonInfo>();
    /// <summary>
    /// Gets a list of directors.
    /// </summary>
    public List<PersonInfo> Writers = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    /// <summary>
    /// Gets a list of genres.
    /// </summary>
    public List<string> Genres = new List<string>();
    public List<string> Languages = new List<string>();

    #region Members

    /// <summary>
    /// Cleans up strings by replacing unwanted characters (<c>'.'</c>, <c>'_'</c>) by spaces.
    /// </summary>
    public static string CleanupWhiteSpaces(string str)
    {
      return str == null ? null : _cleanUpWhiteSpaces.Replace(str, " $1").Trim(' ', '-');
    }

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!AreReqiredFieldsFilled)
        return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, SeriesName.Text);     
      if(!EpisodeName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE_NAME, CleanString(EpisodeName.Text));
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, EpisodeNumbers.Select(e => (object)e).ToList());
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_DVDEPISODE, DvdEpisodeNumbers.Select(e => (object)e).ToList());

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_EPISODE, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_EPISODE, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_EPISODE, TvRageId.ToString());

      if (!string.IsNullOrEmpty(SeriesImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesImdbId);
      if (SeriesTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvdbId.ToString());
      if (SeriesMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesMovieDbId.ToString());
      if (SeriesTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvMazeId.ToString());
      if (SeriesTvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvRageId.ToString());

      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_RATING_COUNT, RatingCount);

      // Construct a "Series Season" string, which will be used for filtering and season banner retrieval.
      int season = SeasonNumber ?? 0;
      string seriesSeason = string.Format(SERIES_SEASON_FORMAT_STR, SeriesName, season.ToString().PadLeft(2, '0'));
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_SEASON, seriesSeason);

      if (!Summary.IsEmpty) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_STORYPLOT, CleanString(Summary.Text));
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList<object>());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_DIRECTORS, Directors.Select(p => p.Name).ToList<object>());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_WRITERS, Writers.Select(p => p.Name).ToList<object>());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList<object>());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_GENRES, Genres.ToList<object>());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, out SeasonNumber);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
      SeriesName = new LanguageText(tempString, false);
      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE_NAME, out tempString);
      EpisodeName = new LanguageText(tempString, false);
      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_STORYPLOT, out tempString);
      Summary = new LanguageText(tempString, false);

      string id;
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_EPISODE, out id))
        TvdbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_EPISODE, out id))
        MovieDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_EPISODE, out id))
        TvMazeId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_EPISODE, out id))
        TvRageId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_EPISODE, out ImdbId);

      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
        SeriesTvdbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
        SeriesMovieDbId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
        SeriesTvMazeId = Convert.ToInt32(id);
      if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
        SeriesTvRageId = Convert.ToInt32(id);
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out SeriesImdbId);

      ICollection<object> collection;
      Actors.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_ACTORS, out collection))
        Actors.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ACTOR }));

      Directors.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_DIRECTORS, out collection))
        Directors.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_DIRECTOR }));

      Writers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_WRITERS, out collection))
        Writers.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_WRITER }));

      Characters.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_CHARACTERS, out collection))
        Characters.AddRange(collection.Select(s => new CharacterInfo() { Name = s.ToString() }));

      Genres.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_GENRES, out collection))
        Genres.AddRange(collection.Select(s => s.ToString()));

      EpisodeNumbers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, out collection))
        EpisodeNumbers.AddRange(collection.Select(s => Convert.ToInt32(s)));

      DvdEpisodeNumbers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_DVDEPISODE, out collection))
        DvdEpisodeNumbers.AddRange(collection.Select(s => Convert.ToDouble(s)));

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        Thumbnail = data;

      if (aspectData.ContainsKey(VideoAudioAspect.ASPECT_ID))
      {
        Languages.Clear();
        IList<MultipleMediaItemAspect> audioAspects;
        if (MediaItemAspect.TryGetAspects(aspectData, VideoAudioAspect.Metadata, out audioAspects))
        {
          foreach (MultipleMediaItemAspect audioAspect in audioAspects)
          {
            string language = audioAspect.GetAttributeValue<string>(VideoAudioAspect.ATTR_AUDIOLANGUAGE);
            if (!string.IsNullOrEmpty(language))
            {
              if (Languages.Contains(language))
                Languages.Add(language);
            }
          }
        }
      }

      return true;
    }

    public string FormatString(string format)
    {
      if (AreReqiredFieldsFilled)
      {
        Match seriesMatch = _fromSeriesName.Match(SeriesName.Text);
        return string.Format(format,
          SeriesFirstAired.HasValue && !seriesMatch.Success ? string.Format(SERIES_FORMAT_STR, SeriesName, SeriesFirstAired.Value.Year) : SeriesName,
          SeasonNumber.ToString().PadLeft(2, '0'),
          StringUtils.Join(",", EpisodeNumbers.OrderBy(e => e).Select(episodeNumber => episodeNumber.ToString().PadLeft(2, '0'))),
          EpisodeName);
      }
      return "EpisodeInfo: No complete match";
    }

    public string ToShortString()
    {
      return FormatString(SHORT_FORMAT_STR);
    }

    public bool FromString(string name)
    {
      Match match = _fromName.Match(name);
      if(match.Success)
      {
        SeriesName = match.Groups["series"].Value;
        Match seriesMatch = _fromSeriesName.Match(SeriesName.Text);
        if (seriesMatch.Success)
        {
          SeriesName = seriesMatch.Groups["series"].Value;
          SeriesFirstAired = new DateTime(Convert.ToInt32(seriesMatch.Groups["year"].Value), 1, 1);
        }
        SeasonNumber = Convert.ToInt32(match.Groups["season"].Value);
        EpisodeNumbers.Clear();
        EpisodeNumbers.Add(Convert.ToInt32(match.Groups["episode"].Value));
        EpisodeName = match.Groups["title"].Value;
        return true;
      }
      return false;
    }

    public bool CopyIdsFrom(SeriesInfo episodeSeries)
    {
      SeriesImdbId = episodeSeries.ImdbId;
      SeriesMovieDbId = episodeSeries.MovieDbId;
      SeriesTvdbId = episodeSeries.TvdbId;
      SeriesTvMazeId = episodeSeries.TvMazeId;
      SeriesTvRageId = episodeSeries.TvRageId;
      return true;
    }

    public bool CopyIdsFrom(SeasonInfo episodeSeason)
    {
      SeriesImdbId = episodeSeason.SeriesImdbId;
      SeriesMovieDbId = episodeSeason.SeriesMovieDbId;
      SeriesTvdbId = episodeSeason.SeriesTvdbId;
      SeriesTvMazeId = episodeSeason.SeriesTvMazeId;
      SeriesTvRageId = episodeSeason.SeriesTvRageId;
      return true;
    }

    public bool CopyIdsFrom(EpisodeInfo otherEpisode)
    {
      MovieDbId = otherEpisode.MovieDbId;
      ImdbId = otherEpisode.ImdbId;
      TvdbId = otherEpisode.TvdbId;
      TvMazeId = otherEpisode.TvMazeId;
      TvRageId = otherEpisode.TvRageId;

      SeriesImdbId = otherEpisode.SeriesImdbId;
      SeriesMovieDbId = otherEpisode.SeriesMovieDbId;
      SeriesTvdbId = otherEpisode.SeriesTvdbId;
      SeriesTvMazeId = otherEpisode.SeriesTvMazeId;
      SeriesTvRageId = otherEpisode.SeriesTvRageId;
      return true;
    }

    public SeriesInfo CloneBasicSeries()
    {
      SeriesInfo info = new SeriesInfo();
      info.ImdbId = SeriesImdbId;
      info.MovieDbId = SeriesMovieDbId;
      info.TvdbId = SeriesTvdbId;
      info.TvMazeId = SeriesTvMazeId;
      info.TvRageId = SeriesTvRageId;

      info.SeriesName = new LanguageText(SeriesName.Text, SeriesName.DefaultLanguage);
      info.FirstAired = SeriesFirstAired;
      return info;
    }

    public SeasonInfo CloneBasicSeason()
    {
      SeasonInfo info = new SeasonInfo();
      info.SeasonNumber = SeasonNumber;

      info.ImdbId = SeriesImdbId;
      info.MovieDbId = SeriesMovieDbId;
      info.TvdbId = SeriesTvdbId;
      info.TvMazeId = SeriesTvMazeId;
      info.TvRageId = SeriesTvRageId;

      info.SeriesName = new LanguageText(SeriesName.Text, SeriesName.DefaultLanguage);
      return info;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return FormatString(EPISODE_FORMAT_STR);
    }

    #endregion
  }
}
