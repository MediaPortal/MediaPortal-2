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
using System.Text.RegularExpressions;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SeasonInfo"/> contains information about a series season. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class SeasonInfo : BaseInfo, IComparable<SeasonInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { SeasonAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Returns the index for "Series" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SERIES_INDEX = 0;
    /// <summary>
    /// Returns the index for "Season" used in <see cref="FormatString"/>.
    /// </summary>
    public static int SEASON_INDEX = 1;
    /// <summary>
    /// Format string that holds series name and season number.
    /// </summary>
    public static string SEASON_FORMAT_STR = "{0} S{1:00}";
    /// <summary>
    /// Short format string that holds season number. Used for browsing seasons by series name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "S{1:00}";

    protected static Regex _fromName = new Regex(@"(?<series>[^\s]*) S(?<season>\d{1,2})", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets or sets the season IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the season TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public int TvRageId = 0;
    public string NameId = null; //Is not saved and only used for comparing/hashing

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
    public string SeriesNameId = null;

    /// <summary>
    /// Gets or sets the series title.
    /// </summary>
    public SimpleTitle SeriesName = null;
    public DateTime? SeriesFirstAired = null;
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets or sets the first aired date of season.
    /// </summary>
    public DateTime? FirstAired = null;
    /// <summary>
    /// Gets or sets the season description.
    /// </summary>
    public SimpleTitle Description = null;
    public int TotalEpisodes = 0;

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages = new List<string>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (SeriesName.IsEmpty)
          return false;
        if (!SeasonNumber.HasValue)
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

        if (SeriesTvdbId > 0)
          return true;
        if (SeriesMovieDbId > 0)
          return true;
        if (SeriesTvMazeId > 0)
          return true;
        if (SeriesTvRageId > 0)
          return true;
        if (!string.IsNullOrEmpty(SeriesImdbId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (string.IsNullOrEmpty(SeriesNameId) && !SeriesName.IsEmpty)
      {
        if (SeriesFirstAired.HasValue)
          SeriesNameId = SeriesName.Text + "(" + SeriesFirstAired.Value.Year + ")";
        else
          SeriesNameId = SeriesName.Text;
        SeriesNameId = GetNameId(SeriesNameId);
      }
      NameId = SeriesNameId + string.Format("S{0}", SeasonNumber.HasValue ? SeasonNumber.Value : 0);
    }

    public SeasonInfo Clone()
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
      if (SeriesName.IsEmpty || !SeasonNumber.HasValue) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(ToString()));
      //MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, true); //Is maintained by medialibrary and metadata extractors
      MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, SeriesName.Text);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_DESCRIPTION, CleanString(Description.Text));
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      if (TotalEpisodes > 0) MediaItemAspect.SetAttribute(aspectData, SeasonAspect.ATTR_NUM_EPISODES, TotalEpisodes);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SEASON, ImdbId);
      if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SEASON, TvdbId.ToString());
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SEASON, MovieDbId.ToString());
      if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SEASON, TvMazeId.ToString());
      if (TvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SEASON, TvRageId.ToString());

      if (!string.IsNullOrEmpty(SeriesImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesImdbId);
      if (SeriesTvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvdbId.ToString());
      if (SeriesMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, SeriesMovieDbId.ToString());
      if (SeriesTvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvMazeId.ToString());
      if (SeriesTvRageId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, SeriesTvRageId.ToString());
      if (!string.IsNullOrEmpty(SeriesNameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, SeriesNameId);

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      GetMetadataChanged(aspectData);

      if (aspectData.ContainsKey(SeasonAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SEASON, out SeasonNumber);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_DESCRIPTION, out tempString);
        Description = new SimpleTitle(tempString, false);

        int? count;
        if (MediaItemAspect.TryGetAttribute(aspectData, SeasonAspect.ATTR_NUM_EPISODES, out count))
          TotalEpisodes = count.Value;

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SEASON, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SEASON, out id))
          TvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SEASON, out ImdbId);

        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesMovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out SeriesImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out SeriesNameId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          HasThumbnail = true;

        return true;
      }
      else if (aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, out SeasonNumber);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
        SeriesName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesMovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvMazeId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVRAGE, ExternalIdentifierAspect.TYPE_SERIES, out id))
          SeriesTvRageId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_SERIES, out SeriesImdbId);

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
      return string.Format(SHORT_FORMAT_STR, SeriesName, SeasonNumber ?? 0);
    }

    public override bool FromString(string name)
    {
      Match match = _fromName.Match(name);
      if (match.Success)
      {
        SeriesName = match.Groups["series"].Value;
        SeasonNumber = Convert.ToInt32(match.Groups["season"].Value);
        return true;
      }
      return false;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is SeriesInfo)
      {
        SeriesInfo seasonSeries = otherInstance as SeriesInfo;
        SeriesImdbId = seasonSeries.ImdbId;
        SeriesMovieDbId = seasonSeries.MovieDbId;
        SeriesTvdbId = seasonSeries.TvdbId;
        SeriesTvMazeId = seasonSeries.TvMazeId;
        SeriesTvRageId = seasonSeries.TvRageId;
        SeriesNameId = seasonSeries.NameId;
        return true;
      }
      else if (otherInstance is SeasonInfo)
      {
        SeasonInfo otherSeason = otherInstance as SeasonInfo;
        MovieDbId = otherSeason.MovieDbId;
        ImdbId = otherSeason.ImdbId;
        TvdbId = otherSeason.TvdbId;
        TvMazeId = otherSeason.TvMazeId;
        TvRageId = otherSeason.TvRageId;

        SeriesImdbId = otherSeason.SeriesImdbId;
        SeriesMovieDbId = otherSeason.SeriesMovieDbId;
        SeriesTvdbId = otherSeason.SeriesTvdbId;
        SeriesTvMazeId = otherSeason.SeriesTvMazeId;
        SeriesTvRageId = otherSeason.SeriesTvRageId;
        SeriesNameId = otherSeason.SeriesNameId;
        return true;
      }
      else if (otherInstance is EpisodeInfo)
      {
        EpisodeInfo seasonEpisode = otherInstance as EpisodeInfo;
        SeriesImdbId = seasonEpisode.SeriesImdbId;
        SeriesMovieDbId = seasonEpisode.SeriesMovieDbId;
        SeriesTvdbId = seasonEpisode.SeriesTvdbId;
        SeriesTvMazeId = seasonEpisode.SeriesTvMazeId;
        SeriesTvRageId = seasonEpisode.SeriesTvRageId;
        SeriesNameId = seasonEpisode.SeriesNameId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(SeriesInfo))
      {
        SeriesInfo info = new SeriesInfo();
        info.ImdbId = SeriesImdbId;
        info.MovieDbId = SeriesMovieDbId;
        info.TvdbId = SeriesTvdbId;
        info.TvMazeId = SeriesTvMazeId;
        info.TvRageId = SeriesTvRageId;
        info.NameId = SeriesNameId;

        info.SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage);
        info.SearchSeason = SeasonNumber;
        info.LastChanged = LastChanged;
        info.DateAdded = DateAdded;
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(SeasonInfo))
      {
        SeasonInfo info = new SeasonInfo();
        info.CopyIdsFrom(this);
        info.SeriesName = SeriesName;
        info.SeasonNumber = SeasonNumber;
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(SeasonInfo))
      {
        SeasonInfo info = new SeasonInfo();
        info.CopyIdsFrom(this);
        info.SeriesName = SeriesName;
        info.SeasonNumber = SeasonNumber;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
       return string.Format(SEASON_FORMAT_STR, SeriesName.IsEmpty ? "[Unnamed Series]" : SeriesName +
         (SeriesFirstAired.HasValue ? string.Format(" ({0})", SeriesFirstAired.Value) : ""), 
          SeasonNumber ?? 0);
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Season]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      SeasonInfo other = obj as SeasonInfo;
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

      if (SeriesTvdbId > 0 && other.SeriesTvdbId > 0 && SeriesTvdbId != other.SeriesTvdbId)
        return false;
      if (SeriesMovieDbId > 0 && other.SeriesMovieDbId > 0 && SeriesMovieDbId != other.SeriesMovieDbId)
        return false;
      if (SeriesTvMazeId > 0 && other.SeriesTvMazeId > 0 && SeriesTvMazeId != other.SeriesTvMazeId)
        return false;
      if (SeriesTvRageId > 0 && other.SeriesTvRageId > 0 && SeriesTvRageId != other.SeriesTvRageId)
        return false;
      if (!string.IsNullOrEmpty(SeriesImdbId) && !string.IsNullOrEmpty(other.SeriesImdbId) &&
        !string.Equals(SeriesImdbId, other.SeriesImdbId, StringComparison.InvariantCultureIgnoreCase))
        return false;
      if (!string.IsNullOrEmpty(SeriesNameId) && !string.IsNullOrEmpty(other.SeriesNameId) &&
        !string.Equals(SeriesNameId, other.SeriesNameId, StringComparison.InvariantCultureIgnoreCase))
        return false;

      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty && SeriesName.Text == other.SeriesName.Text &&
        SeasonNumber.HasValue && SeasonNumber == other.SeasonNumber)
        return true;

      return false;
    }

    public int CompareTo(SeasonInfo other)
    {
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty && SeriesName.Text != other.SeriesName.Text)
        return SeriesName.Text.CompareTo(other.SeriesName.Text);
      if (SeasonNumber.HasValue && other.SeasonNumber.HasValue && SeasonNumber.Value != other.SeasonNumber.Value)
        return SeasonNumber.Value.CompareTo(other.SeasonNumber.Value);

      return 0;
    }

    #endregion
  }
}
