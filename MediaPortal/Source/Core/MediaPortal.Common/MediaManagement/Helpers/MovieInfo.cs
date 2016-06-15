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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Text.RegularExpressions;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="MovieInfo"/> contains information about a movie. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class MovieInfo : BaseInfo, IComparable<MovieInfo>
  {
    /// <summary>
    /// Returns the index for "Movie" used in <see cref="FormatString"/>.
    /// </summary>
    public static int MOVIE_INDEX = 0;
    /// <summary>
    /// Returns the index for "Year" used in <see cref="FormatString"/>.
    /// </summary>
    public static int MOVIE_YEAR_INDEX = 1;
    /// <summary>
    /// Format string that holds movie name including premiere year.
    /// </summary>
    public static string MOVIE_FORMAT_STR = "{0} ({1})";
    /// <summary>
    /// Short format string that holds movie name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0}";

    protected static Regex _fromName = new Regex(@"(?<movie>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);

    public int MovieDbId = 0;
    public string ImdbId = null;

    public LanguageText MovieName = null;
    public string OriginalName = null;
    public DateTime? ReleaseDate = null;
    public int Runtime = 0;
    public string Certification = null;
    public string Tagline = null;
    public LanguageText Summary = null;

    public LanguageText CollectionName = null;
    public int CollectionMovieDbId = 0;

    public float Popularity = 0;
    public long Budget = 0;
    public long Revenue = 0;
    public double Score = 0;
    public double TotalRating = 0;
    public int RatingCount = 0;
    public int Order = int.MaxValue;

    /// <summary>
    /// Contains a list of <see cref="CultureInfo.TwoLetterISOLanguageName"/> of the medium. This can be used
    /// to do an online lookup in the best matching language.
    /// </summary>
    public List<string> Languages = new List<string>();
    public List<PersonInfo> Actors = new List<PersonInfo>();
    public List<PersonInfo> Directors = new List<PersonInfo>();
    public List<PersonInfo> Writers = new List<PersonInfo>();
    public List<CharacterInfo> Characters = new List<CharacterInfo>();
    public List<CompanyInfo> ProductionCompanies = new List<CompanyInfo>();
    public List<string> Genres = new List<string>();
    public List<string> Awards = new List<string>();

    #region Members

    /// <summary>
    /// Copies the contained movie information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (MovieName.IsEmpty) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, MovieName.Text);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (!Summary.IsEmpty) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_STORYPLOT, CleanString(Summary.Text));
      if (!string.IsNullOrEmpty(Tagline)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TAGLINE, Tagline);
      if (!CollectionName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, CollectionName.Text);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_CERTIFICATION, Certification);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, ImdbId);
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, MovieDbId.ToString());
      if (CollectionMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionMovieDbId.ToString());

      if (Runtime > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RUNTIME_M, Runtime);
      if (Budget > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_BUDGET, Budget);
      if (Revenue > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_REVENUE, Revenue);

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_POPULARITY, Popularity);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_SCORE, Score);
      if (TotalRating > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, TotalRating);
      if (RatingCount > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, RatingCount);
      
      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_ACTORS, Actors.Select(p => p.Name).ToList<object>());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_DIRECTORS, Directors.Select(p => p.Name).ToList<object>());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_WRITERS, Writers.Select(p => p.Name).ToList<object>());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_CHARACTERS, Characters.Select(p => p.Name).ToList<object>());

      if (Genres.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_GENRES, Genres.ToList<object>());
      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_AWARDS, Awards.ToList<object>());

      if (ProductionCompanies.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_COMPANIES, ProductionCompanies.Select(c => c.Name).ToList<object>());

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (aspectData.ContainsKey(MovieAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_TAGLINE, out Tagline);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_CERTIFICATION, out Certification);

        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_RUNTIME_M, out Runtime);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_BUDGET, out Budget);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_REVENUE, out Revenue);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_POPULARITY, out Popularity);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_SCORE, out Score);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, out TotalRating);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, out RatingCount);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, out tempString);
        MovieName = new LanguageText(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_STORYPLOT, out tempString);
        Summary = new LanguageText(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, out tempString);
        CollectionName = new LanguageText(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          CollectionMovieDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);

        ICollection<object> collection;
        Actors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_ACTORS, out collection))
          Actors.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_ACTOR }));

        Directors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_DIRECTORS, out collection))
          Directors.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_DIRECTOR }));

        Writers.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_WRITERS, out collection))
          Writers.AddRange(collection.Select(s => new PersonInfo() { Name = s.ToString(), Occupation = PersonAspect.OCCUPATION_WRITER }));

        Characters.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_CHARACTERS, out collection))
          Characters.AddRange(collection.Select(s => new CharacterInfo() { Name = s.ToString() }));

        Genres.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_GENRES, out collection))
          Genres.AddRange(collection.Select(s => s.ToString()));

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Select(s => s.ToString()));

        ProductionCompanies.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COMPANIES, out collection))
          ProductionCompanies.AddRange(collection.Select(s => new CompanyInfo() { Name = s.ToString(), Type = CompanyAspect.COMPANY_PRODUCTION }));

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
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out tempString);
        MovieName = new LanguageText(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          CollectionMovieDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);

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
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, MovieName);
    }

    public bool FromString(string name)
    {
      if (name.Contains("("))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          MovieName = match.Groups["movie"].Value;
          int year = Convert.ToInt32(match.Groups["year"].Value);
          if (year > 0)
            ReleaseDate = new DateTime(year, 1, 1);
          return true;
        }
        return false;
      }
      MovieName = name;
      return true;
    }

    public bool CopyIdsFrom(MovieInfo otherMovie)
    {
      MovieDbId = otherMovie.MovieDbId;
      ImdbId = otherMovie.ImdbId;
      CollectionMovieDbId = otherMovie.CollectionMovieDbId;
      return true;
    }

    public MovieCollectionInfo CloneBasicMovieCollection()
    {
      MovieCollectionInfo info = new MovieCollectionInfo();
      info.MovieDbId = CollectionMovieDbId;
      info.CollectionName = new LanguageText(CollectionName.Text, CollectionName.DefaultLanguage);
      return info;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      //if (ReleaseDate.HasValue)
      //  return string.Format(MOVIE_FORMAT_STR, MovieName, ReleaseDate.Value.Year);
      return MovieName.Text;
    }

    public override bool Equals(object obj)
    {
      MovieInfo other = obj as MovieInfo;
      if (obj == null) return false;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId) return true;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId) &&
        string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase))
        return true;
      if (!MovieName.IsEmpty && !other.MovieName.IsEmpty &&
        MatchNames(MovieName.Text, other.MovieName.Text))
        return true;

      return false;
    }

    public int CompareTo(MovieInfo other)
    {
      if (Order != other.Order)
        return Order.CompareTo(other.Order);
      if (MovieName.IsEmpty || other.MovieName.IsEmpty)
        return 1;

      return MovieName.Text.CompareTo(other.MovieName.Text);
    }

    #endregion
  }
}
