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

using System.Reflection;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data
{
  //{
  //  "Title": "Battleship",
  //  "Year": "2012",
  //  "Rated": "PG-13",
  //  "Released": "18 May 2012",
  //  "Runtime": "131 min",
  //  "Genre": "Action, Adventure, Sci-Fi",
  //  "Director": "Peter Berg",
  //  "Writer": "Jon Hoeber, Erich Hoeber",
  //  "Actors": "Taylor Kitsch, Alexander Skarsg�rd, Rihanna, Brooklyn Decker",
  //  "Plot": "Based on the classic Hasbro naval combat game, Battleship is the story of an international fleet of ships who come across an alien armada while on Naval war games exercise. An intense battle is fought on sea, land and air. What do the aliens want?",
  //  "Language": "English, Japanese",
  //  "Country": "USA",
  //  "Awards": "4 wins & 14 nominations.",
  //  "Poster": "http://ia.media-imdb.com/images/M/MV5BMjI5NTM5MDA2N15BMl5BanBnXkFtZTcwNjkwMzQxNw@@._V1_SX300.jpg",
  //  "Metascore": "41",
  //  "imdbRating": "5.9",
  //  "imdbVotes": "197,704",
  //  "imdbID": "tt1440129",
  //  "Type": "movie",
  //  "tomatoMeter": "34",
  //  "tomatoImage": "rotten",
  //  "tomatoRating": "4.6",
  //  "tomatoReviews": "209",
  //  "tomatoFresh": "71",
  //  "tomatoRotten": "138",
  //  "tomatoConsensus": "It may offer energetic escapism for less demanding filmgoers, but Battleship is too loud, poorly written, and formulaic to justify its expense -- and a lot less fun than its source material.",
  //  "tomatoUserMeter": "55",
  //  "tomatoUserRating": "3.3",
  //  "tomatoUserReviews": "455950",
  //  "tomatoURL": "http://www.rottentomatoes.com/m/battleship/",
  //  "DVD": "28 Aug 2012",
  //  "BoxOffice": "$65.2M",
  //  "Production": "Universal",
  //  "Website": "http://www.battleshipmovie.com/",
  //  "Response": "True"
  //}
  [DataContract]
  public class OmDbMovie : OmDbBaseItem
  {
  }
}
