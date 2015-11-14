#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.DLNA;

namespace MediaPortal.Plugins.MediaServer.Objects.MediaLibrary
{
  public class MediaLibraryMusicTrack : MediaLibraryAudioItem, IDirectoryMusicTrack
  {
    public MediaLibraryMusicTrack(string baseKey, MediaItem item, EndPointSettings client)
      : base(baseKey, item, client)
    {
      DlnaMediaItem dlnaItem = client.GetDlnaItem(item, false);

      Artist = new List<string>();
      Album = new List<string>();
      Playlist = new List<string>();
      Contributor = new List<string>();

      var audioAspect = item.Aspects[AudioAspect.ASPECT_ID];
      var album = audioAspect.GetAttributeValue(AudioAspect.ATTR_ALBUM);
      if (album != null) Album.Add(album.ToString());

      var artists = audioAspect.GetCollectionAttribute(AudioAspect.ATTR_ARTISTS);
      if (artists != null) Artist = new List<string>(artists.Cast<string>());

      var composers = audioAspect.GetCollectionAttribute(AudioAspect.ATTR_COMPOSERS);
      if (composers != null) Contributor = new List<string>(composers.Cast<string>());

      var originalTrack = audioAspect.GetAttributeValue(AudioAspect.ATTR_TRACK);
      if (originalTrack != null) OriginalTrackNumber = Convert.ToInt32(originalTrack.ToString());

      object oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_RECORDINGTIME);
      if (oValue != null)
      {
        Date = Convert.ToDateTime(oValue).Date.ToString("yyyy-MM-dd");
      }
    }

    public override string Class
    {
      get { return "object.item.audioItem.musicTrack"; }
    }

    public IList<string> Artist { get; set; }

    public IList<string> Album { get; set; }

    public int OriginalTrackNumber { get; set; }

    public IList<string> Playlist { get; set; }

    public string StorageMedium { get; set; }

    public IList<string> Contributor { get; set; }

    public string Date { get; set; }
  }
}
