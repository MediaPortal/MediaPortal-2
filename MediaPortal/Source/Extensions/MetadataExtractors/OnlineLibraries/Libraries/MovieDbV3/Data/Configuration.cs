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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
  //{
  //  "images": {
  //      "backdrop_sizes": ["w300", "w780", "w1280", "original"],
  //      "base_url": "http://cf2.imgobject.com/t/p/",
  //      "poster_sizes": ["w92", "w154", "w185", "w342", "w500", "original"],
  //      "profile_sizes": ["w45", "w185", "h632", "original"]
  //  }
  //}
  [DataContract]
  public class Configuration
  {
    [DataMember(Name = "images")]
    public ImageConfiguration Images { get; set; }
  }

  [DataContract]
  public class ImageConfiguration
  {
    [DataMember(Name = "backdrop_sizes")]
    public List<string> BackdropSizes { get; set; }

    [DataMember(Name = "poster_sizes")]
    public List<string> PosterSizes { get; set; }

    [DataMember(Name = "profile_sizes")]
    public List<string> ProfileSizes { get; set; }

    [DataMember(Name = "base_url")]
    public string BaseUrl { get; set; }

    public override string ToString()
    {
      return BaseUrl;
    }
  }
}
