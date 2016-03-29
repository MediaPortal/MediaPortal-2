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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data
{
  [DataContract]
  public class MovieThumbs
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "tmdb_id")]
    public string TheMovieDbID { get; set; }

    [DataMember(Name = "imdb_id")]
    public string ImDbID { get; set; }

    [DataMember(Name = "hdmovielogo")]
    public List<MovieThumb> HDMovieLogos { get; set; }

    [DataMember(Name = "moviedisc")]
    public List<MovieDiscThumb> MovieCDArt { get; set; }

    [DataMember(Name = "movielogo")]
    public List<MovieThumb> MovieLogos { get; set; }

    [DataMember(Name = "movieposter")]
    public List<MovieThumb> MoviePosters { get; set; }

    [DataMember(Name = "hdmovieclearart")]
    public List<MovieThumb> HDMovieClearArt { get; set; }

    [DataMember(Name = "movieart")]
    public List<MovieThumb> MovieClearArt { get; set; }

    [DataMember(Name = "moviebackground")]
    public List<MovieThumb> MovieFanArt { get; set; }

    [DataMember(Name = "moviebanner")]
    public List<MovieThumb> MovieBanners { get; set; }

    [DataMember(Name = "moviethumb")]
    public List<MovieThumb> MovieThumbnails { get; set; }
  }
}
