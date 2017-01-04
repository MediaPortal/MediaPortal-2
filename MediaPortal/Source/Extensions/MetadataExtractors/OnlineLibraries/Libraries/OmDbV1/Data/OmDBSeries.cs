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

using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
  //{
  //  "Title": "Game of Thrones",
  //  "Year": "2011�",
  //  "Rated": "TV-MA",
  //  "Released": "17 Apr 2011",
  //  "Runtime": "56 min",
  //  "Genre": "Adventure, Drama, Fantasy",
  //  "Director": "N/A",
  //  "Writer": "David Benioff, D.B. Weiss",
  //  "Actors": "Peter Dinklage, Lena Headey, Emilia Clarke, Kit Harington",
  //  "Plot": "Ned Stark, Lord of Winterfell, becomes the Hand of the King after the former Hand, Jon Arryn, has passed away...",
  //  "Language": "English",
  //  "Country": "USA",
  //  "Awards": "Won 1 Golden Globe. Another 172 wins & 278 nominations.",
  //  "Poster": "http://ia.media-imdb.com/images/M/MV5BMjM5OTQ1MTY5Nl5BMl5BanBnXkFtZTgwMjM3NzMxODE@._V1_SX300.jpg",
  //  "Metascore": "N/A",
  //  "imdbRating": "9.5",
  //  "imdbVotes": "934,499",
  //  "imdbID": "tt0944947",
  //  "Type": "series",
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
  public class OmDbSeries : OmDbBaseItem
  {
  }
}
