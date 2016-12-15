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
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
  //{
  //  "Title": "Winter Is Coming",
  //  "Year": "2011",
  //  "Rated": "TV-MA",
  //  "Released": "17 Apr 2011",
  //  "Season": "1",
  //  "Episode": "1",
  //  "Runtime": "62 min",
  //  "Genre": "Adventure, Drama, Fantasy",
  //  "Director": "Timothy Van Patten",
  //  "Writer": "David Benioff (created by), D.B. Weiss (created by), George R.R. Martin (\"A Song of Ice and Fire\" by), David Benioff, D.B. Weiss",
  //  "Actors": "Sean Bean, Mark Addy, Nikolaj Coster-Waldau, Michelle Fairley",
  //  "Plot": "In the land of Winterfell, Lord Ned Stark begins to believe that something is amiss. A deserter from the Night Watch...",
  //  "Language": "English",
  //  "Country": "USA",
  //  "Awards": "N/A",
  //  "Poster": "http://ia.media-imdb.com/images/M/MV5BMTk5MDU3OTkzMF5BMl5BanBnXkFtZTcwOTc0ODg5NA@@._V1_SX300.jpg",
  //  "Metascore": "N/A",
  //  "imdbRating": "8.9",
  //  "imdbVotes": "14119",
  //  "imdbID": "tt1480055",
  //  "seriesID": "tt0944947",
  //  "Type": "episode",
  //  "tomatoMeter": "N/A",
  //  "tomatoImage": "N/A",
  //  "tomatoRating": "N/A",
  //  "tomatoReviews": "N/A",
  //  "tomatoFresh": "N/A",
  //  "tomatoRotten": "N/A",
  //  "tomatoConsensus": "N/A",
  //  "tomatoUserMeter": "N/A",
  //  "tomatoUserRating": "N/A",
  //  "tomatoUserReviews": "N/A",
  //  "tomatoURL": "N/A",
  //  "DVD": "N/A",
  //  "BoxOffice": "N/A",
  //  "Production": "N/A",
  //  "Website": "N/A",
  //  "Response": "True"
  //}
  [DataContract]
  public class OmDbEpisode : OmDbBaseItem
  {
    [DataMember(Name = "Season")]
    public string SeasonNumberStr
    {
      set
      {
        int season;
        if (int.TryParse(value, out season))
          SeasonNumber = season;
      }
    }

    public int? SeasonNumber { get; set; }

    [DataMember(Name = "Episode")]
    public string EpisodeNumberStr
    {
      set
      {
        int episode;
        if (int.TryParse(value, out episode))
          EpisodeNumber = episode;
      }
    }

    public int? EpisodeNumber { get; set; }

    [DataMember(Name = "seriesID")]
    public string ImdbSeriesID { get; set; }
  }
}
