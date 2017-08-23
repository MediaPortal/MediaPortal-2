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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data
{
  //    {
  //      "id": "12355",
  //      "url": "http://assets.fanart.tv/fanart/movies/120/moviebanner/the-lord-of-the-rings-the-fellowship-of-the-ring-50485f0da465c.jpg",
  //      "lang": "en",
  //      "likes": "1"
  //    }
  [DataContract]
  public class FanArtMovieThumb : FanArtThumb
  {
    public FanArtMovieThumb()
    { }

    public FanArtMovieThumb(FanArtThumb thumb)
    {
      Id = thumb.Id;
      Url = thumb.Url;
      Likes = thumb.Likes;
      Language = null;
    }

    [DataMember(Name = "lang")]
    public string Language { get; set; }
  }
}
