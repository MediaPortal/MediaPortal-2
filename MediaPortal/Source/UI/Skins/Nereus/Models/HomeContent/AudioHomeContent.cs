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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class AudioHomeContent : AbstractHomeContent
  {
    public AudioHomeContent()
    {
      _availableLists.Add(new LatestAudioList());
      _availableLists.Add(new ContinueAlbumList());
      _availableLists.Add(new FavoriteAudioList());
      _availableLists.Add(new UnplayedAlbumList());
    }

    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new AudioGenreShortcut(),
        new AudioTrackShortcut(),
        new AudioAlbumShortcut(),
        new AudioArtistShortcut(),
        new AudioYearShortcut()
      }));
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  public class LatestAudioList : MediaListItemsListWrapper
  {
    public LatestAudioList()
      : base("LatestAudio", "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueAlbumList : MediaListItemsListWrapper
  {
    public ContinueAlbumList()
      : base("ContinuePlayAlbum", "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteAudioList : MediaListItemsListWrapper
  {
    public FavoriteAudioList()
      : base("FavoriteAudio", "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedAlbumList : MediaListItemsListWrapper
  {
    public UnplayedAlbumList()
      : base("UnplayedAlbum", "[Nereus.Home.Unplayed]")
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

  public class AudioArtistShortcut : ArtistShortcutItem
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
