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
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Text.RegularExpressions;
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="MovieInfo"/> contains information about a movie. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class MovieInfo : BaseInfo, IComparable<MovieInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { MovieAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
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
    public int AllocinebId = 0;
    public int CinePassionId = 0;
    public string NameId = null;

    public SimpleTitle MovieName = null;
    public SimpleTitle MovieNameSort = new SimpleTitle();
    public string OriginalName = null;
    public DateTime? ReleaseDate = null;
    public int Runtime = 0;
    public string Certification = null;
    public string Tagline = null;
    public SimpleTitle Summary = null;

    public SimpleTitle CollectionName = null;
    public int CollectionMovieDbId = 0;
    public string CollectionNameId = null;

    public float Popularity = 0;
    public long Budget = 0;
    public long Revenue = 0;
    public double Score = 0;
    public SimpleRating Rating = new SimpleRating();
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
    public List<GenreInfo> Genres = new List<GenreInfo>();
    public List<string> Awards = new List<string>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (MovieName.IsEmpty)
          return false;
        if (Runtime == 0)
          return false;
        if (!ReleaseDate.HasValue)
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (MovieDbId > 0)
          return true;
        if (AllocinebId > 0)
          return true;
        if (CinePassionId > 0)
          return true;
        if (!string.IsNullOrEmpty(ImdbId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!MovieName.IsEmpty)
      {
        if (ReleaseDate.HasValue)
          NameId = MovieName.Text + "(" + ReleaseDate.Value.Year + ")";
        else
          NameId = MovieName.Text;
        NameId = GetNameId(NameId);
      }
      if (!CollectionName.IsEmpty)
      {
        CollectionNameId = GetNameId(CollectionName.Text);
      }
    }

    public MovieInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained movie information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (MovieName.IsEmpty) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      if (!MovieNameSort.IsEmpty)
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, MovieNameSort.Text);
      else
        MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(MovieName.Text));
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, IsVirtualResource(aspectData));
      MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, MovieName.Text);
      if(!string.IsNullOrEmpty(OriginalName)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_ORIG_MOVIE_NAME, OriginalName);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (!Summary.IsEmpty) MediaItemAspect.SetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, CleanString(Summary.Text));
      if (!string.IsNullOrEmpty(Tagline)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TAGLINE, Tagline);
      if (!CollectionName.IsEmpty) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, CollectionName.Text);
      if (!string.IsNullOrEmpty(Certification)) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_CERTIFICATION, Certification);

      if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, ImdbId);
      if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, MovieDbId.ToString());
      if (AllocinebId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_ALLOCINE, ExternalIdentifierAspect.TYPE_MOVIE, AllocinebId.ToString());
      if (CinePassionId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CINEPASSION, ExternalIdentifierAspect.TYPE_MOVIE, CinePassionId.ToString());
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_MOVIE, NameId);

      if (CollectionMovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionMovieDbId.ToString());
      if (!string.IsNullOrEmpty(CollectionNameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COLLECTION, CollectionNameId);

      if (Runtime > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RUNTIME_M, Runtime);
      if (Budget > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_BUDGET, Budget);
      if (Revenue > 0) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_REVENUE, Revenue);

      if (Popularity > 0f) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_POPULARITY, Popularity);
      if (Score > 0d) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_SCORE, Score);

      if (!Rating.IsEmpty)
      {
        MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      if (Actors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_ACTORS, Actors.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (Directors.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, Directors.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (Writers.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_WRITERS, Writers.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());
      if (Characters.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, Characters.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).ToList());

      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_AWARDS, Awards.Where(a => !string.IsNullOrEmpty(a)).ToList());
      if (ProductionCompanies.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, MovieAspect.ATTR_COMPANIES, ProductionCompanies.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name).ToList());

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

        double? rating;
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_TOTAL_RATING, out rating);
        int? voteCount;
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_RATING_COUNT, out voteCount);
        Rating = new SimpleRating(rating, voteCount);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_MOVIE_NAME, out tempString);
        MovieName = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, out tempString);
        MovieNameSort = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_STORYPLOT, out tempString);
        Summary = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COLLECTION_NAME, out tempString);
        CollectionName = new SimpleTitle(tempString, false);
        MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_ORIG_MOVIE_NAME, out tempString);
        OriginalName = tempString;

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ALLOCINE, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          AllocinebId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CINEPASSION, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          CinePassionId = Convert.ToInt32(id);

        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          CollectionMovieDbId = Convert.ToInt32(id);

        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_MOVIE, out NameId);

        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COLLECTION, out CollectionNameId);

        //Brownard 17.06.2016
        //The returned type of the collection differs on the server and client.
        //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
        //Use the non generic Ienumerable to allow for both types.
        IEnumerable collection;
        Actors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_ACTORS, out collection))
          Actors.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ACTOR, MediaName = MovieName.Text }));
        foreach (PersonInfo actor in Actors)
          actor.AssignNameId();

        Directors.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_DIRECTORS, out collection))
          Directors.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_DIRECTOR, MediaName = MovieName.Text }));
        foreach (PersonInfo director in Directors)
          director.AssignNameId();

        Writers.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_WRITERS, out collection))
          Writers.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_WRITER, MediaName = MovieName.Text }));
        foreach (PersonInfo writer in Writers)
          writer.AssignNameId();

        Characters.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, VideoAspect.ATTR_CHARACTERS, out collection))
          Characters.AddRange(collection.Cast<string>().Select(s => new CharacterInfo { Name = s, MediaName = MovieName.Text }));
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
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Cast<string>());

        ProductionCompanies.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, MovieAspect.ATTR_COMPANIES, out collection))
          ProductionCompanies.AddRange(collection.Cast<string>().Select(s => new CompanyInfo { Name = s, Type = CompanyAspect.COMPANY_PRODUCTION }));
        foreach (CompanyInfo company in ProductionCompanies)
          company.AssignNameId();

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
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out tempString);
        MovieName = new SimpleTitle(tempString, false);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ALLOCINE, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          AllocinebId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CINEPASSION, ExternalIdentifierAspect.TYPE_MOVIE, out id))
          CinePassionId = Convert.ToInt32(id);

        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COLLECTION, out id))
          CollectionMovieDbId = Convert.ToInt32(id);

        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_MOVIE, out NameId);

        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COLLECTION, out CollectionNameId);

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
      return string.Format(SHORT_FORMAT_STR, MovieName);
    }

    public override bool FromString(string name)
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

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is MovieInfo)
      {
        MovieInfo otherMovie = otherInstance as MovieInfo;
        MovieDbId = otherMovie.MovieDbId;
        AllocinebId = otherMovie.AllocinebId;
        CinePassionId = otherMovie.CinePassionId;
        ImdbId = otherMovie.ImdbId;
        NameId = otherMovie.NameId;
        CollectionMovieDbId = otherMovie.CollectionMovieDbId;
        CollectionNameId = otherMovie.CollectionNameId;
        return true;
      }
      else if (otherInstance is MovieCollectionInfo)
      {
        MovieCollectionInfo otherCollection = otherInstance as MovieCollectionInfo;
        CollectionMovieDbId = otherCollection.MovieDbId;
        CollectionNameId = otherCollection.NameId;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(MovieCollectionInfo))
      {
        MovieCollectionInfo info = new MovieCollectionInfo();
        info.MovieDbId = CollectionMovieDbId;
        info.NameId = CollectionNameId;
        info.CollectionName = new SimpleTitle(CollectionName.Text, CollectionName.DefaultLanguage);
        info.Languages.AddRange(Languages);
        info.LastChanged = LastChanged;
        info.DateAdded = DateAdded;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      if (ReleaseDate.HasValue)
        return string.Format(MOVIE_FORMAT_STR, MovieName.IsEmpty ? "[Unnamed Movie]" : MovieName.Text, ReleaseDate.Value.Year);
      return MovieName.IsEmpty ? "[Unnamed Movie]" : MovieName.Text;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Movie]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      MovieInfo other = obj as MovieInfo;
      if (other == null) return false;

      if (MovieDbId > 0 && other.MovieDbId > 0)
        return MovieDbId == other.MovieDbId;
      if (AllocinebId > 0 && other.AllocinebId > 0)
        return AllocinebId == other.AllocinebId;
      if (CinePassionId > 0 && other.CinePassionId > 0)
        return CinePassionId == other.CinePassionId;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId))
        return string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase);

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      if (!MovieName.IsEmpty && !other.MovieName.IsEmpty && MatchNames(MovieName.Text, other.MovieName.Text))
        return true;

      return false;
    }

    public int CompareTo(MovieInfo other)
    {
      if (Order != other.Order)
        return Order.CompareTo(other.Order);
      if (ReleaseDate.HasValue && other.ReleaseDate.HasValue && ReleaseDate.Value != other.ReleaseDate.Value)
        return ReleaseDate.Value.CompareTo(other.ReleaseDate.Value);
      if (MovieName.IsEmpty || other.MovieName.IsEmpty)
        return 1;

      return MovieName.Text.CompareTo(other.MovieName.Text);
    }

    #endregion
  }
}
