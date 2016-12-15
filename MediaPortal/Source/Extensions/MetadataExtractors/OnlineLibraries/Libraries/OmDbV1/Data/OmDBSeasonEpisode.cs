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
  //    {
  //      "Title": "Winter Is Coming",
  //      "Released": "2011-04-17",
  //      "Episode": "1",
  //      "imdbRating": "8.9",
  //      "imdbID": "tt1480055"
  //    }
  [DataContract]
  public class OmDbSeasonEpisode : OmDbBaseResponse
  {
    [DataMember(Name = "Title")]
    public string Title { get; set; }

    [DataMember(Name = "Released")]
    public string ReleasedStr
    {
      get
      {
        if (Released.HasValue)
          return Released.ToString();
        return null;
      }
      set
      {
        DateTime releaseDate;
        if (DateTime.TryParse(value, out releaseDate))
          Released = releaseDate;
        Released = null;
      }
    }

    public DateTime? Released { get; set; }

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

    [DataMember(Name = "imdbRating")]
    public string ImdbRating { get; set; }

    [DataMember(Name = "imdbID")]
    public string ImdbID { get; set; }
  }
}
