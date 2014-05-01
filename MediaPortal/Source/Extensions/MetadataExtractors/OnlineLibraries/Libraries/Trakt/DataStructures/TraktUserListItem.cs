using System;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Trakt.DataStructures
{
  [DataContract]
  public class TraktUserListItem
  {
    [DataMember(Name = "type")]
    public string Type { get; set; }

    [DataMember(Name = "movie")]
    public TraktMovie Movie { get; set; }

    [DataMember(Name = "show")]
    public TraktShow Show { get; set; }

    [DataMember(Name = "episode")]
    public TraktEpisode Episode { get; set; }

    /// <summary>
    /// all the authentication properties (plays, watched etc) are not correctly set 
    /// for each corresponding type (movie,show,episode)...this has been reported
    /// we can workaround this for now. They are currently only set on the root item
    /// </summary>

    #region Helpers

    public TraktRatings Ratings
    {
      get
      {
        TraktRatings retValue = null;

        switch (Type)
        {
          case "movie":
            retValue = Movie.Ratings;
            break;

          case "show":
          case "season":
            retValue = Show.Ratings;
            break;
          case "episode":
            retValue = Episode.Ratings;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.Ratings = value;
            break;

          case "show":
          case "season":
            Show.Ratings = value;
            break;
          case "episode":
            Episode.Ratings = value;
            break;
        }
      }
    }

    [DataMember(Name = "rating")]
    public string Rating
    {
      get
      {
        string retValue = "false";

        switch (Type)
        {
          case "movie":
            retValue = Movie.Rating;
            break;

          case "show":
          case "season":
            retValue = Show.Rating;
            break;
          case "episode":
            retValue = Episode.Rating;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.Rating = value;
            break;

          case "show":
          case "season":
            Show.Rating = value;
            break;
          case "episode":
            Episode.Rating = value;
            break;
        }
      }
    }

    [DataMember(Name = "rating_advanced")]
    public int RatingAdvanced
    {
      get
      {
        int retValue = 0;

        switch (Type)
        {
          case "movie":
            retValue = Movie.RatingAdvanced;
            break;

          case "show":
          case "season":
            retValue = Show.RatingAdvanced;
            break;
          case "episode":
            retValue = Episode.RatingAdvanced;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.RatingAdvanced = value;
            break;

          case "show":
          case "season":
            Show.RatingAdvanced = value;
            break;
          case "episode":
            Episode.RatingAdvanced = value;
            break;
        }
      }
    }

    [DataMember(Name = "plays")]
    public int Plays
    {
      get
      {
        int retValue = 0;

        switch (Type)
        {
          case "movie":
            retValue = Movie.Plays;
            break;

          case "show":
            retValue = Show.Plays;
            break;

          case "season":
            retValue = _plays;
            break;
          case "episode":
            retValue = Episode.Plays;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.Plays = value;
            break;

          case "show":
            Show.Plays = value;
            break;

          case "season":
            _plays = value;
            break;

          case "episode":
            Episode.Plays = value;
            break;
        }
      }
    } int _plays;

    [DataMember(Name = "watched")]
    public bool Watched
    {
      get
      {
        bool retValue = false;

        switch (Type)
        {
          case "movie":
            retValue = Movie.Watched;
            break;

          case "show":
            retValue = Show.Watched;
            break;

          case "season":
            retValue = _watched;
            break;

          case "episode":
            retValue = Episode.Watched;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.Watched = value;
            break;

          case "show":
            Show.Watched = value;
            break;

          case "season":
            _watched = value;
            break;

          case "episode":
            Episode.Watched = value;
            break;
        }
      }
    } bool _watched;

    [DataMember(Name = "in_collection")]
    public bool InCollection
    {
      get
      {
        bool retValue = false;

        switch (Type)
        {
          case "movie":
            retValue = Movie.InCollection;
            break;

          case "show":
          case "season":
            retValue = _incollection;
            break;
          case "episode":
            retValue = Episode.InCollection;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.InCollection = value;
            break;

          case "show":
          case "season":
            _incollection = value;
            break;
          case "episode":
            Episode.InCollection = value;
            break;
        }
      }
    } bool _incollection;

    [DataMember(Name = "in_watchlist")]
    public bool InWatchList
    {
      get
      {
        bool retValue = false;

        switch (Type)
        {
          case "movie":
            retValue = Movie.InWatchList;
            break;

          case "show":
          case "season":
            retValue = _inwatchlist;
            break;
          case "episode":
            retValue = Episode.InWatchList;
            break;
        }
        return retValue;
      }
      set
      {
        switch (Type)
        {
          case "movie":
            Movie.InWatchList = value;
            break;

          case "show":
          case "season":
            _inwatchlist = value;
            break;
          case "episode":
            Episode.InWatchList = value;
            break;
        }
      }
    } bool _inwatchlist;

    [DataMember(Name = "episode_num")]
    public string EpisodeNumber
    {
      get
      {
        return _episodeNumber;
      }
      set
      {
        if (Type == "episode")
        {
          Episode = Episode ?? new TraktEpisode();
          Episode.Number = Convert.ToInt32(value);
        }
        _episodeNumber = value;
      }
    } string _episodeNumber = null;

    [DataMember(Name = "season")]
    public string SeasonNumber
    {
      get
      {
        return _season;
      }
      set
      {
        if (Type == "episode")
        {
          Episode = Episode ?? new TraktEpisode();
          Episode.Season = Convert.ToInt32(value);
        }
        _season = value;
      }
    } string _season = null;

    public string Year
    {
      get
      {
        string retValue = string.Empty;

        switch (Type)
        {
          case "movie":
            retValue = Movie.Year;
            break;

          case "show":
          case "season":
          case "episode":
            retValue = Show.Year.ToString();
            break;
        }
        return retValue;
      }
    }

    public string Title
    {
      get
      {
        string retValue = string.Empty;

        switch (Type)
        {
          case "movie":
            retValue = Movie.Title;
            break;

          case "show":
          case "season":
          case "episode":
            retValue = Show.Title;
            break;
        }
        return retValue;
      }
    }

    public string ImdbId
    {
      get
      {
        string retValue = string.Empty;

        switch (Type)
        {
          case "movie":
            retValue = Movie.IMDBID;
            break;

          case "show":
          case "season":
          case "episode":
            retValue = Show.Imdb;
            break;
        }
        return retValue;
      }
    }

    #endregion
  }
}