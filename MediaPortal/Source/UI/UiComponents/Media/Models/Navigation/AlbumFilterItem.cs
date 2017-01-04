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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Settings;

namespace MediaPortal.UiComponents.Media.Models.Navigation
{
  /// <summary>
  /// Holds a GUI item which represents an audio album filter choice.
  /// </summary>
  public class AlbumFilterItem : PlayableContainerMediaItem
  {
    public override void Update(MediaItem mediaItem)
    {
      base.Update(mediaItem);

      AlbumInfo album = new AlbumInfo();
      if (!album.FromMetadata(mediaItem.Aspects))
        return;

      Album = album.Album ?? "";
      Description = album.Description.Text ?? "";

      int? count;
      if (mediaItem.Aspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAlbumAspect.ATTR_AVAILABLE_TRACKS, out count))
          AvailableTracks = count.Value.ToString();
        else
          AvailableTracks = "";

        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, AudioAlbumAspect.ATTR_NUMTRACKS, out count))
          TotalTracks = count.Value.ToString();
        else
          TotalTracks = "";

        if (ShowVirtualSetting.ShowVirtualAudioMedia)
          Tracks = TotalTracks;
        else
          Tracks = AvailableTracks;
      }

      FireChange();
    }

    public string Album
    {
      get { return this[Consts.KEY_ALBUM]; }
      set { SetLabel(Consts.KEY_ALBUM, value); }
    }

    public string Description
    {
      get { return this[Consts.KEY_DESCRIPTION]; }
      set { SetLabel(Consts.KEY_DESCRIPTION, value); }
    }

    public string AvailableTracks
    {
      get { return this[Consts.KEY_AVAIL_TRACKS]; }
      set { SetLabel(Consts.KEY_AVAIL_TRACKS, value); }
    }

    public string TotalTracks
    {
      get { return this[Consts.KEY_TOTAL_TRACKS]; }
      set { SetLabel(Consts.KEY_TOTAL_TRACKS, value); }
    }

    public string Tracks
    {
      get { return this[Consts.KEY_NUM_TRACKS]; }
      set { SetLabel(Consts.KEY_NUM_TRACKS, value); }
    }
  }
}
