#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class AudioHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new AudioGenreShortcut(),
        new AudioTrackShortcut(),
        new AudioAlbumShortcut(),
        new AudioArtistShortcut(),
        new AudioYearShortcut()
      }));

      _backingList.Add(new LatestAudioList(mlm.Lists["LatestAudio"].AllItems));
      _backingList.Add(new ContinueAlbumList(mlm.Lists["ContinuePlayAlbum"].AllItems));
      _backingList.Add(new FavoriteAudioList(mlm.Lists["FavoriteAudio"].AllItems));
      _backingList.Add(new UnplayedAlbumList(mlm.Lists["UnplayedAlbum"].AllItems));
    }
  }

  public class LatestAudioList : ItemsListWrapper
  {
    public LatestAudioList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueAlbumList : ItemsListWrapper
  {
    public ContinueAlbumList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteAudioList : ItemsListWrapper
  {
    public FavoriteAudioList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedAlbumList : ItemsListWrapper
  {
    public UnplayedAlbumList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Unplayed]")
    { }
  }

  public class AudioGenreShortcut : GenreShortcutItem
  {
    public AudioGenreShortcut()
      : base(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, typeof(AudioFilterByGenreScreenData))
    { }
  }

  public class AudioTrackShortcut : MediaScreenShortcutItem
  {
    public AudioTrackShortcut()
      : base(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, typeof(AudioShowItemsScreenData))
    { }
  }

  public class AudioAlbumShortcut : MediaScreenShortcutItem
  {
    public AudioAlbumShortcut()
      : base(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, typeof(AudioFilterByAlbumScreenData))
    { }
  }

  public class AudioArtistShortcut : ActorShortcutItem
  {
    public AudioArtistShortcut()
      : base(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, typeof(AudioFilterByAlbumArtistScreenData))
    { }
  }

  public class AudioYearShortcut : YearShortcutItem
  {
    public AudioYearShortcut()
      : base(Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT, typeof(AudioFilterByDecadeScreenData))
    { }
  }
}
