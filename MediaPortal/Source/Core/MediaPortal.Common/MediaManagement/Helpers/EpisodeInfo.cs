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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Utilities;
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="EpisodeInfo"/> contains metadata information about a series episode item.
  /// </summary>
  /// <remarks>
  /// If all required fields are filled, the <see cref="AreReqiredFieldsFilled"/> 
  /// returns <c>true</c>. The <see cref="ToString"/> method returns a well formatted series title if <see cref="AreReqiredFieldsFilled"/> is <c>true</c>.
  /// </remarks>
  public class EpisodeInfo : BaseInfo, IComparable<EpisodeInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { EpisodeAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
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

    protected static Regex _fromName = new Regex(@"(?<series>[^\s]*) S(?<season>\d{1,2})E(?<episode>\d{1,2}).* - (?<title>.*)", RegexOptions.IgnoreCase);
    protected static Regex _fromSeriesName = new Regex(@"(?<series>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets or sets the episode IMDB id.
    /// </summary>
    public string ImdbId = null;
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
    public string SeriesAlternateName = null;
    public DateTime? SeriesFirstAired = null;
    /// <summary>
    /// Gets or sets the episode title.
    /// </summary>
    public SimpleTitle EpisodeName = null;
    public SimpleTitle EpisodeNameSort = new SimpleTitle();
    /// <summary>
    /// Gets or sets the season number. A "0" value will be treated as valid season number.
    /// </summary>
    public int? SeasonNumber = null;
    /// <summary>
    /// Gets a list of episode numbers.
    /// </summary>
    public List<int> EpisodeNumbers = new List<int>();
    /// <summary>
    /// Gets the first episode number in ascending order.
    /// </summary>
    public int FirstEpisodeNumber
    {
      get { return EpisodeNumbers.OrderBy(e => e).FirstOrDefault(); }
    }
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
    public SimpleTitle Summary = null;
    public SimpleRating Rating = new SimpleRating();

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
    public List<GenreInfo> Genres = new List<GenreInfo>();
    public List<string> Languages = new List<string>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (SeriesName.IsEmpty)
          return false;
        if (!SeasonNumber.HasValue)
          return false;
        if (EpisodeNumbers.Count == 0)
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
      NameId = SeriesNameId + string.Format("S{0}E{1}", SeasonNumber.HasValue ? SeasonNumber.Value : 0, FirstEpisodeNumber);
    }

    public EpisodeInfo Clone()
    {
      EpisodeInfo clone = (EpisodeInfo)this.MemberwiseClone();
      clone.SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage);
      clone.EpisodeName = new SimpleTitle(EpisodeName.Text, EpisodeName.DefaultLanguage);
      clone.EpisodeNameSort = new SimpleTitle(EpisodeNameSort.Text, EpisodeNameSort.DefaultLanguage);
      clone.Summary = new SimpleTitle(Summary.Text, Summary.DefaultLanguage);
      clone.Rating = new SimpleRating(Rating.RatingValue, Rating.VoteCount);
      clone.Languages = new List<string>();
      foreach (var l in Languages)
        clone.Languages.Add(l);
      clone.EpisodeNumbers = new List<int>();
      foreach (var e in EpisodeNumbers)
        clone.EpisodeNumbers.Add(e);
      clone.DvdEpisodeNumbers = new List<double>();
      foreach (var de in DvdEpisodeNumbers)
        clone.DvdEpisodeNumbers.Add(de);
      clone.Actors = new List<PersonInfo>();
      foreach (var a in Actors)
        clone.Actors.Add(a.Clone());
      clone.Directors = new List<PersonInfo>();
      foreach (var d in Directors)
        clone.Directors.Add(d.Clone());
      clone.Writers = new List<PersonInfo>();
      foreach (var w in Writers)
        clone.Writers.Add(w.Clone());
      clone.Characters = new List<CharacterInfo>();
      foreach (var c in Characters)
        clone.Characters.Add(c.Clone());
      clone.Genres = new List<GenreInfo>();
      foreach (var g in Genres)
        clone.Genres.Add(new GenreInfo() { Id = g.Id, Name = g.Name });

      return clone;
    }

    public override bool MergeWith(object other, bool overwriteShorterStrings = true, bool updatePrimaryChildList = false)
    {
      if (other is EpisodeInfo episode)
      {
        bool forceMerge = false;
        //Merge of single and multi-episode needs to force overwrite of properties
        if (EpisodeNumbers.Count == 1 && episode.EpisodeNumbers.Count > 1)
          forceMerge = true;

        HasChanged |= MetadataUpdater.SetOrUpdateId(ref ImdbId, episode.ImdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref MovieDbId, episode.MovieDbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref TvdbId, episode.TvdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref TvMazeId, episode.TvMazeId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref TvRageId, episode.TvRageId);

        HasChanged |= MetadataUpdater.SetOrUpdateId(ref SeriesImdbId, episode.SeriesImdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref SeriesMovieDbId, episode.SeriesMovieDbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref SeriesTvdbId, episode.SeriesTvdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref SeriesTvMazeId, episode.SeriesTvMazeId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref SeriesTvRageId, episode.SeriesTvRageId);

        if (forceMerge)
        {
          //Merge of single and multi-episode needs to force overwrite of name and summery
          EpisodeName = episode.EpisodeName;
          Summary = episode.Summary;
          HasChanged = true;
        }
        else
        {
          HasChanged |= MetadataUpdater.SetOrUpdateString(ref EpisodeName, episode.EpisodeName, overwriteShorterStrings);
          HasChanged |= MetadataUpdater.SetOrUpdateString(ref Summary, episode.Summary, overwriteShorterStrings);
        }
        HasChanged |= MetadataUpdater.SetOrUpdateString(ref SeriesName, episode.SeriesName, overwriteShorterStrings);

        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref FirstAired, episode.FirstAired);
        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref SeasonNumber, episode.SeasonNumber);
        MetadataUpdater.SetOrUpdateValue(ref SeriesFirstAired, episode.SeriesFirstAired);

        HasChanged |= MetadataUpdater.SetOrUpdateRatings(ref Rating, episode.Rating);

        if (EpisodeNumbers.Count == 0 || forceMerge)
          HasChanged |= MetadataUpdater.SetOrUpdateList(EpisodeNumbers, episode.EpisodeNumbers.Distinct().ToList(), true);
        if (DvdEpisodeNumbers.Count == 0 || forceMerge)
          HasChanged |= MetadataUpdater.SetOrUpdateList(DvdEpisodeNumbers, episode.DvdEpisodeNumbers.Distinct().ToList(), true);
        if (Genres.Count == 0)
          HasChanged |= MetadataUpdater.SetOrUpdateList(Genres, episode.Genres.Distinct().ToList(), true);

        if (!HasThumbnail && episode.HasThumbnail)
        {
          Thumbnail = episode.Thumbnail;
          HasChanged = true;
        }

        //These lists contain Ids and other properties that are not persisted, so they will always appear changed.
        //So changes to these lists will only be stored if something else has changed.
        MetadataUpdater.SetOrUpdateList(Actors, episode.Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), Actors.Count == 0, overwriteShorterStrings);
        MetadataUpdater.SetOrUpdateList(Characters, episode.Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), Characters.Count == 0, overwriteShorterStrings);
        MetadataUpdater.SetOrUpdateList(Directors, episode.Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), Directors.Count == 0, overwriteShorterStrings);
        MetadataUpdater.SetOrUpdateList(Writers, episode.Writers.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), Writers.Count == 0, overwriteShorterStrings);
        return true;
      }
      return false;
    }

    #region Members

    /// <summary>
    /// Copies the contained series information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData, bool force = false)
    {
      if (!force && !IsBaseInfoPresent)
        return false;

      AssignNameId();
      SetMetadataChanged(aspectData);
      EpisodeName.Text = CleanString(EpisodeName.Text);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      if (!EpisodeNameSort.IsEmpty) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, EpisodeNameSort.Text);
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, IsVirtualResource(aspectData));
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, SeriesName.Text);
      if (!EpisodeName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE_NAME, EpisodeName.Text);
      if (SeasonNumber.HasValue) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, SeasonNumber.Value);
      if (FirstAired.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, FirstAired.Value);
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, EpisodeNumbers.Distinct());
      MediaItemAspect.SetCollectionAttribute(aspectData, EpisodeAspect.ATTR_DVDEPISODE, DvdEpisodeNumbers.Distinct());

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
      if (!string.IsNullOrEmpty(SeriesNameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, SeriesNameId);

      if (!Rating.IsEmpty)
      {
        MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      // Construct a "Series Season" string, which will be used for filtering and season banner retrieval.
      int season = SeasonNumber ?? 0;
      string seriesSeason = string.Format(SERIES_SEASON_FORMAT_STR, SeriesName, season.ToString().PadLeft(2, '0'));
      MediaItemAspect.SetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_SEASON, seriesSeason);

      bool? isDvd;
      if (!MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_ISDVD, out isDvd) || !isDvd.HasValue)
        MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_ISDVD, false);

      if (!Summary.IsEmpty) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, CleanString(Summary.Text));
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).Distinct().ToList());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).Distinct().ToList());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).Distinct().ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).Distinct().ToList());

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
      if (!aspectData.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      GetMetadataChanged(aspectData);

      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SEASON, out SeasonNumber);
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out FirstAired);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_SERIES_NAME, out tempString);
      SeriesName = new SimpleTitle(tempString, string.IsNullOrWhiteSpace(tempString));
      MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE_NAME, out tempString);
      EpisodeName = new SimpleTitle(tempString, string.IsNullOrWhiteSpace(tempString));
      MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, out tempString);
      EpisodeNameSort = new SimpleTitle(tempString, string.IsNullOrWhiteSpace(tempString));
      MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, out tempString);
      Summary = new SimpleTitle(tempString, string.IsNullOrWhiteSpace(tempString));

      double value;
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_TOTAL_RATING, out value))
        Rating.RatingValue = value;

      int count;
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_RATING_COUNT, out count))
        Rating.VoteCount = count;

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
      MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_SERIES, out SeriesNameId);

      //Brownard 17.06.2016
      //The returned type of the collection differs on the server and client.
      //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
      //Use the non generic Ienumerable to allow for both types.
      IEnumerable collection;
      Actors.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_ACTORS, out collection))
        Actors.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ACTOR, MediaName = EpisodeName.Text, ParentMediaName = SeriesName.Text }));
      foreach (PersonInfo actor in Actors)
        actor.AssignNameId();

      Directors.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, out collection))
        Directors.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_DIRECTOR, MediaName = EpisodeName.Text, ParentMediaName = SeriesName.Text }));
      foreach (PersonInfo director in Directors)
        director.AssignNameId();

      Writers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_WRITERS, out collection))
        Writers.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_WRITER, MediaName = EpisodeName.Text, ParentMediaName = SeriesName.Text }));
      foreach (PersonInfo writer in Writers)
        writer.AssignNameId();

      Characters.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, out collection))
        Characters.AddRange(collection.Cast<string>().Select(s => new CharacterInfo { Name = s, MediaName = EpisodeName.Text, ParentMediaName = SeriesName.Text }));
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

      EpisodeNumbers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, out collection))
        CollectionUtils.AddAll(EpisodeNumbers, collection.Cast<int>().Distinct());

      DvdEpisodeNumbers.Clear();
      if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_DVDEPISODE, out collection))
        DvdEpisodeNumbers.AddRange(collection.Cast<double>().Distinct());

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
            if (!string.IsNullOrEmpty(language) && !Languages.Contains(language))
            {
              Languages.Add(language);
            }
          }
        }
      }
      return true;
    }

    public string FormatString(string format)
    {
      if (!IsBaseInfoPresent)
        return "EpisodeInfo: No complete match";
      Match seriesMatch = _fromSeriesName.Match(SeriesName.Text);
      return string.Format(format,
        SeriesFirstAired.HasValue && !seriesMatch.Success ? string.Format(SERIES_FORMAT_STR, SeriesName, SeriesFirstAired.Value.Year) : SeriesName,
        SeasonNumber.ToString().PadLeft(2, '0'),
        StringUtils.Join(",", EpisodeNumbers.OrderBy(e => e).Select(episodeNumber => episodeNumber.ToString().PadLeft(2, '0'))),
        EpisodeName);
    }

    public string ToShortString()
    {
      return FormatString(SHORT_FORMAT_STR);
    }

    public override bool FromString(string name)
    {
      Match match = _fromName.Match(name);
      if (match.Success)
      {
        SeriesName = match.Groups["series"].Value;
        Match seriesMatch = _fromSeriesName.Match(SeriesName.Text);
        if (seriesMatch.Success)
        {
          //SeriesName = seriesMatch.Groups["series"].Value;
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

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is SeriesInfo)
      {
        SeriesInfo episodeSeries = otherInstance as SeriesInfo;
        SeriesImdbId = episodeSeries.ImdbId;
        SeriesMovieDbId = episodeSeries.MovieDbId;
        SeriesTvdbId = episodeSeries.TvdbId;
        SeriesTvMazeId = episodeSeries.TvMazeId;
        SeriesTvRageId = episodeSeries.TvRageId;
        SeriesNameId = episodeSeries.NameId;
        return true;
      }
      else if (otherInstance is SeasonInfo)
      {
        SeasonInfo episodeSeason = otherInstance as SeasonInfo;
        SeriesImdbId = episodeSeason.SeriesImdbId;
        SeriesMovieDbId = episodeSeason.SeriesMovieDbId;
        SeriesTvdbId = episodeSeason.SeriesTvdbId;
        SeriesTvMazeId = episodeSeason.SeriesTvMazeId;
        SeriesTvRageId = episodeSeason.SeriesTvRageId;
        SeriesNameId = episodeSeason.SeriesNameId;
        return true;
      }
      else if (otherInstance is EpisodeInfo)
      {
        EpisodeInfo otherEpisode = otherInstance as EpisodeInfo;
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
        SeriesNameId = otherEpisode.SeriesNameId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(SeriesInfo))
      {
        SeriesInfo info = new SeriesInfo
        {
          ImdbId = SeriesImdbId,
          MovieDbId = SeriesMovieDbId,
          TvdbId = SeriesTvdbId,
          TvMazeId = SeriesTvMazeId,
          TvRageId = SeriesTvRageId,
          NameId = SeriesNameId,
          SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage),
          AlternateName = SeriesAlternateName,
          FirstAired = SeriesFirstAired,
          SearchSeason = SeasonNumber,
          SearchEpisode = EpisodeNumbers != null && EpisodeNumbers.Count > 0 ? (int?)FirstEpisodeNumber : null,
          LastChanged = LastChanged,
          DateAdded = DateAdded
        };
        info.Languages.AddRange(Languages);
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(SeasonInfo))
      {
        SeasonInfo info = new SeasonInfo
        {
          SeasonNumber = SeasonNumber,
          SeriesImdbId = SeriesImdbId,
          SeriesMovieDbId = SeriesMovieDbId,
          SeriesTvdbId = SeriesTvdbId,
          SeriesTvMazeId = SeriesTvMazeId,
          SeriesTvRageId = SeriesTvRageId,
          SeriesNameId = SeriesNameId,
          SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage),
          SeriesFirstAired = SeriesFirstAired,
          LastChanged = LastChanged,
          DateAdded = DateAdded
        };
        info.Languages.AddRange(Languages);
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(EpisodeInfo))
      {
        EpisodeInfo info = new EpisodeInfo();
        info.CopyIdsFrom(this);
        info.SeriesName = new SimpleTitle(SeriesName.Text, SeriesName.DefaultLanguage);
        info.SeasonNumber = SeasonNumber;
        info.EpisodeNumbers = EpisodeNumbers.ToList();
        info.EpisodeName = new SimpleTitle(EpisodeName.Text, EpisodeName.DefaultLanguage);
        info.EpisodeNameSort = new SimpleTitle(EpisodeName.Text, EpisodeName.DefaultLanguage);
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return FormatString(EPISODE_FORMAT_STR);
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Episode]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      EpisodeInfo other = obj as EpisodeInfo;
      if (obj == null) return false;

      bool multiEpisodeComparison = false;
      if ((EpisodeNumbers.Count > 1 || other.EpisodeNumbers.Count > 1) &&
        EpisodeNumbers.Intersect(other.EpisodeNumbers).Any() && EpisodeNumbers.Count != other.EpisodeNumbers.Count)
        multiEpisodeComparison = true;

      if (!multiEpisodeComparison)
      {
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
      }

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

      bool matchSeriesId = false;
      if (SeriesTvdbId > 0 && other.SeriesTvdbId > 0)
        matchSeriesId |= SeriesTvdbId == other.SeriesTvdbId;
      if (SeriesMovieDbId > 0 && other.SeriesMovieDbId > 0)
        matchSeriesId |= SeriesMovieDbId == other.SeriesMovieDbId;
      if (SeriesTvMazeId > 0 && other.SeriesTvMazeId > 0)
        matchSeriesId |= SeriesTvMazeId == other.SeriesTvMazeId;
      if (SeriesTvRageId > 0 && other.SeriesTvRageId > 0)
        matchSeriesId |= SeriesTvRageId == other.SeriesTvRageId;
      if (!string.IsNullOrEmpty(SeriesImdbId) && !string.IsNullOrEmpty(other.SeriesImdbId))
        matchSeriesId |= string.Equals(SeriesImdbId, other.SeriesImdbId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(SeriesNameId) && !string.IsNullOrEmpty(other.SeriesNameId))
        matchSeriesId |= string.Equals(SeriesNameId, other.SeriesNameId, StringComparison.InvariantCultureIgnoreCase);
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty)
        matchSeriesId |= SeriesName.Text == other.SeriesName.Text;

      if (matchSeriesId &&
        SeasonNumber.HasValue && other.SeasonNumber.HasValue && SeasonNumber.Value == other.SeasonNumber.Value &&
        EpisodeNumbers.Count > 0 && other.EpisodeNumbers.Count > 0 && EpisodeNumbers.Intersect(other.EpisodeNumbers).Any())
        return true;
      if (matchSeriesId &&
        SeasonNumber.HasValue && other.SeasonNumber.HasValue && SeasonNumber.Value == other.SeasonNumber.Value &&
        !EpisodeName.IsEmpty && !other.EpisodeName.IsEmpty && MatchNames(EpisodeName.Text, other.EpisodeName.Text))
        return true;
      if (SeasonNumber.HasValue && other.SeasonNumber.HasValue && SeasonNumber.Value == other.SeasonNumber.Value &&
        EpisodeNumbers.Count > 0 && other.EpisodeNumbers.Count > 0 && EpisodeNumbers.Intersect(other.EpisodeNumbers).Any() &&
        !EpisodeName.IsEmpty && !other.EpisodeName.IsEmpty && MatchNames(EpisodeName.Text, other.EpisodeName.Text))
        return true;

      return false;
    }

    public int CompareTo(EpisodeInfo other)
    {
      if (!SeriesName.IsEmpty && !other.SeriesName.IsEmpty && SeriesName.Text != other.SeriesName.Text)
        return SeriesName.Text.CompareTo(other.SeriesName.Text);
      if (SeasonNumber.HasValue && other.SeasonNumber.HasValue && SeasonNumber.Value != other.SeasonNumber.Value)
        return SeasonNumber.Value.CompareTo(other.SeasonNumber.Value);
      if (EpisodeNumbers.Count > 0 && other.EpisodeNumbers.Count > 0 && EpisodeNumbers.First() != other.EpisodeNumbers.First())
        return EpisodeNumbers.First().CompareTo(other.EpisodeNumbers.First());
      if (EpisodeName.IsEmpty || other.EpisodeName.IsEmpty)
        return 1;

      return EpisodeName.Text.CompareTo(other.EpisodeName.Text);
    }

    #endregion
  }
}
