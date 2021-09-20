#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Music
{
  public class WebMusicTrackBasic : WebMediaItem, IYearSortable, IGenreSortable, IRatingSortable, IMusicTrackNumberSortable
  {
    public WebMusicTrackBasic()
    {
      ArtistId = new List<string>();
      Genres = new List<string>();
    }

    public string AlbumArtist { get; set; }
    public string AlbumArtistId { get; set; }
    public IList<string> Artist { get; set; }
    public IList<string> ArtistId { get; set; }
    public string Album { get; set; }
    public string AlbumId { get; set; }
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public int Year { get; set; }
    public int Duration { get; set; }
    public float Rating { get; set; }
    public IList<string> Genres { get; set; }

    public override WebMediaType Type
    {
      get { return WebMediaType.MusicTrack; }
    }

    public override string ToString()
    {
      return Title;
    }
  }
}
