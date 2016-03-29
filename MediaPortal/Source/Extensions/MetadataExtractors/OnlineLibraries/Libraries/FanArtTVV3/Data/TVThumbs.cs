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
  public class TVThumbs
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "thetvdb_id")]
    public string TheTVDbID { get; set; }

    [DataMember(Name = "hdtvlogo")]
    public List<MovieThumb> HDShowLogos { get; set; }

    [DataMember(Name = "characterart")]
    public List<MovieDiscThumb> ShowCharacterArt { get; set; }

    [DataMember(Name = "clearlogo")]
    public List<MovieThumb> ShowLogos { get; set; }

    [DataMember(Name = "seasonposter")]
    public List<SeasonThumb> SeasonPosters { get; set; }

    [DataMember(Name = "tvposter")]
    public List<MovieThumb> ShowPosters { get; set; }

    [DataMember(Name = "hdclearart")]
    public List<MovieThumb> HDShowClearArt { get; set; }

    [DataMember(Name = "clearart")]
    public List<MovieThumb> ShowClearArt { get; set; }

    [DataMember(Name = "showbackground")]
    public List<MovieThumb> ShowFanArt { get; set; }

    [DataMember(Name = "tvbanner")]
    public List<MovieThumb> ShowBanners { get; set; }

    [DataMember(Name = "seasonbanner")]
    public List<SeasonThumb> SeasonBanners { get; set; }

    [DataMember(Name = "tvthumb")]
    public List<MovieThumb> ShowThumbnails { get; set; }

    [DataMember(Name = "seasonthumb")]
    public List<SeasonThumb> SeasonThumbnails { get; set; }
  }
}
