#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManagement;

namespace Models.Music
{
  /// <summary>
  /// Model which exposes a movie collection
  /// The movie collection are just movies & folders on the HDD
  /// </summary>
  public class Model
  {
    public const string IMPORTERSQUEUE_NAME = "Importers";
    public const string MEDIAMANAGERQUEUE_NAME = "MediaManager";

    #region variables

    private ItemsCollection _sortMenu;
    private ItemsCollection _mainMenu;
    private ItemsCollection _viewsMenu;
    private ItemsCollection _songs;
    private MusicSettings _settings;
    private readonly MusicFactory _factory;
    private FolderItem _folder;
    private readonly IList<IAbstractMediaItem> _musicViews;
    private ListItem _selectedItem;
    private IList<IMenuItem> _dynamicContextMenuItems;
    private IMetaDataMappingCollection _currentMap;

    enum ContextMenuItem
    {
      AddToPlayList = 0,
      AddAllToPlaylist = 1,
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
      //load our settings

      _songs = new ItemsCollection();
      _factory = new MusicFactory();

      _settings = new MusicSettings();

      // get music-views
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      _musicViews = mediaManager.GetView("/Music");
      _currentMap = _musicViews[0].Mapping;

      //get settings
      _settings = ServiceScope.Get<ISettingsManager>().Load<MusicSettings>();
      if (_settings.Folder == "")
      {
        SelectView(Views[0]);
      }
      else
      {
        _factory.LoadSongs(ref _songs, ref _currentMap, _settings.Sort, _settings.Folder);
        if (_songs.Count == 0)
        {
          SelectView(Views[0]);
        }
      }

      if (_songs.Count > 0)
      {
        FolderItem f = _songs[_songs.Count - 1] as FolderItem;
        MusicItem p = _songs[_songs.Count - 1] as MusicItem;
        if (f != null)
        {
          _folder = new FolderItem(f.MediaContainer.Parent);
        }
        if (p != null)
        {
          _folder = new FolderItem(p.MediaItem.Parent);
        }
      }

      //register for messages from players and mediamanager

      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = broker.GetOrCreate(MEDIAMANAGERQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnMediaManagerMessageReceived);

      queue = broker.GetOrCreate(IMPORTERSQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnImporterMessageReceived);

      //create our dynamic context menu items
      _dynamicContextMenuItems = new List<IMenuItem>();


      StringId menuText = new StringId("playlists", "add");
      MenuItem menuItem = new MenuItem(menuText, "");
      menuItem.Command = "music:Model.AddToPlayList+ScreenManager.CloseDialog";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("playlists", "addall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "music:Model.AddAllToPlayList+ScreenManager.CloseDialog";
      _dynamicContextMenuItems.Add(menuItem);
    }

    void OnImporterMessageReceived(QueueMessage message)
    {
      Refresh();
      _songs.FireChange();
    }

    #region music collection methods
    /// <summary>
    /// exposes the main menu to the skin.
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("music-main"));

        return _mainMenu;
      }
    }

    /// <summary>
    /// exposes the context menu to the skin
    /// </summary>
    /// <value>The context menu.</value>
    public ItemsCollection ContextMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("music-contextmenu"));

      }
    }

    /// <summary>
    /// provides a collection of songs to the skin
    /// </summary>
    /// <value>The songs.</value>
    public ItemsCollection Songs
    {
      get
      {
        // FIXME: Do not open a dialog during the evaluation of a property, this has to be
        // solved in another way. Same in MyPicture class.
        //IImporterManager importer = ServiceScope.Get<IImporterManager>();
        //if (importer.Shares.Count == 0)
        //{
        //  ServiceScope.Get<IScreenManager>().ShowDialog("dialogNoSharesDefined");
        //  Refresh();
        //}
        return _songs;
      }
    }

    /// <summary>
    /// Refreshes the view.
    /// </summary>
    private void Refresh()
    {
      //load movie collection for current folder
      _factory.LoadSongs(ref _songs, _folder, ref _currentMap, _settings.Sort);

      //sort it
      _songs.Sort(new MusicComparer(_settings.Sort, _currentMap));
    }
    /// <summary>
    /// Called when a message has been received from the mediamanager
    /// We check if the media manager send a container-changed message
    /// and ifso refresh the movies collection
    /// </summary>
    /// <param name="message">The message.</param>
    void OnMediaManagerMessageReceived(QueueMessage message)
    {
      if (_folder != null && _folder.MediaContainer != null)
      {
        if (message.MessageData.ContainsKey("action"))
        {
          if (message.MessageData["action"].ToString() == "changed")
          {
            if (message.MessageData.ContainsKey("fullpath"))
            {
              if (message.MessageData["fullpath"].ToString() == _folder.MediaContainer.FullPath)
              {
                Refresh();
                _songs.FireChange();
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Expose playdvd command to skin
    /// Plays the DVD.
    /// </summary>
    public void PlayDvd() { }

    /// <summary>
    /// Expose Eject command to skin
    /// Ejects the cd/dvd from the drive
    /// </summary>
    public void Eject() { }

    /// <summary>
    /// provides a command for the skin to select a song
    /// if its a folder, we build a new collection showing the contents of the folder
    /// if its a song , we play it
    /// </summary>
    /// <param name="item">The item.</param>
    public void Select(ListItem item)
    {
      if (item == null)
      {
        //nothing selected
        return;
      }
      //did user select a folder ?
      if ((item as FolderItem) != null)
      {
        // yes then load the folder and return its items
        _folder = (FolderItem)item;
        _settings = ServiceScope.Get<ISettingsManager>().Load<MusicSettings>();
        if (_folder.MediaContainer != null)
          _settings.Folder = _folder.MediaContainer.FullPath;
        else
          _settings.Folder = "";
        ServiceScope.Get<ISettingsManager>().Save(_settings);
        Refresh();
        _songs.FireChange();
        return;
      }
      else
      {
        //the user clicked on a media item
        MusicItem song = (MusicItem)item;
        Uri uri = song.MediaItem.ContentUri;
        if (uri == null)
        {
          return; // no uri? then return
        }

        //we dont handle .jpg or .png (yet)
        // Todo: Check for Valid Audio File
        if (uri.AbsoluteUri.IndexOf(".jpg") < 0 && uri.AbsoluteUri.IndexOf(".png") < 0)
        {
          //seems this is a movie, lets play it
          try
          {
            //show waitcursor
            //window.WaitCursorVisible = true;

            //stop any other movies
            IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
            //create a new player for our movie
            IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
            IPlayer player = factory.GetPlayer(song.MediaItem);
            if (collection.Count > 0 && collection[0].IsVideo)
            {
              collection.Dispose();
            }

            if (player != null)
            {
              //add it to the player collection
              collection.Add(player);
              //play it
              player.Play(song.MediaItem);
            }
            else
              ServiceScope.Get<ILogger>().Debug("Music: No Player found for {0}", song.MediaItem.ContentUri);
          }
          finally
          {
            //hide waitcursor
            //window.WaitCursorVisible = false;
          }
        }
      }
    }

    #endregion

    #region sorting methods


    /// <summary>
    /// Provides a list of sort options to the skin
    /// (used in dialogmenu.xml)
    /// </summary>
    /// <value>The sort options.</value>
    public ItemsCollection SortOptions
    {
      get
      {
        _sortMenu = new ItemsCollection();

        if (_currentMap != null)
        {
          foreach (IMetadataMapping map in _currentMap.Mappings)
          {
            _sortMenu.Add(new ListItem("Name", map.LocalizedName.ToString()));
          }
          SetSelectedSortMode();
        }
        return _sortMenu;
      }
    }

    void SetSelectedSortMode()
    {
      for (int i = 0; i < _sortMenu.Count; ++i)
      {
        if (i != (int)_settings.Sort)
          _sortMenu[i].Selected = false;
        else
          _sortMenu[i].Selected = true;
      }
    }

    /// <summary>
    /// provides command for the skin to sort the current movie collection
    /// </summary>
    /// <param name="selectedItem">The item.</param>
    public void Sort(ListItem selectedItem)
    {
      for (int i = 0; i < _sortMenu.Count; ++i)
      {
        if (selectedItem == _sortMenu[i])
        {
          _settings.Sort = i;
          ServiceScope.Get<ISettingsManager>().Save(_settings);
          Refresh();
          _songs.FireChange();
        }
      }
      SetSelectedSortMode();
    }

    #endregion

    #region view methods

    /// <summary>
    /// Exposes a collection containing all view-names to the skin
    /// </summary>
    /// <value>The views.</value>
    public ItemsCollection Views
    {
      get
      {
        if (_viewsMenu == null)
        {
          _viewsMenu = new ItemsCollection();
          foreach (IAbstractMediaItem view in _musicViews)
          {
            _viewsMenu.Add(new ListItem("Name", view.Title));
          }
        }
        SetSelectedView();

        return _viewsMenu;
      }
    }

    void SetSelectedView()
    {
      if (_folder == null) return;
      if (_folder.MediaContainer == null) return;
      for (int i = 0; i < _viewsMenu.Count; ++i)
      {
        _viewsMenu[i].Selected = false;
      }
      for (int i = 0; i < _musicViews.Count; ++i)
      {
        if (_musicViews[i].FullPath == _folder.MediaContainer.FullPath)
        {
          _viewsMenu[i].Selected = true;
        }
      }
    }

    /// <summary>
    /// Called by the skin if the user wants to view a different view
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void SelectView(ListItem selectedItem)
    {
      if (selectedItem == null) return;
      foreach (IAbstractMediaItem item in _musicViews)
      {
        if (item.Title == selectedItem.Labels["Name"].Evaluate())
        {
          SetSelectedView();
          _settings.Folder = item.FullPath;
          ServiceScope.Get<ISettingsManager>().Save(_settings);

          ListItem selectedView = Views[SelectedViewIndex];
          string viewName = selectedView.ToString();
          _factory.LoadSongs(ref _songs, ref _currentMap, _settings.Sort, item.FullPath);
          _songs.Sort(new MusicComparer(_settings.Sort, _currentMap));
          _songs.FireChange();
          return;
        }
      }
      //shares selected
      _folder = null;
      Refresh();
      _songs.FireChange();
      ServiceScope.Get<ISettingsManager>().Save(_settings);
    }

    int SelectedViewIndex
    {
      get
      {
        for (int i = 0; i < Views.Count; ++i)
        {
          if (Views[i].Selected) return i;
        }
        return 0;
      }
    }

    #endregion

    /// <summary>
    /// updates the context menu depending on what item is currently selected
    /// </summary>
    void UpdateContextMenu()
    {
      IMenu menu = ServiceScope.Get<IMenuCollection>().GetMenu("music-contextmenu");
      foreach (IMenuItem menuItem in _dynamicContextMenuItems)
      {
        menu.Items.Remove(menuItem);
      }

      if (SelectedItem != null)
      {
        menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.AddAllToPlaylist]);
        if ((SelectedItem as FolderItem) != null)
        {

        }
        else
        {
          MusicItem song = SelectedItem as MusicItem;
          if (song != null)
          {
            menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.AddToPlayList]);
          }
        }
      }
    }

    public void OnSelectionChange(ListItem item)
    {
      SelectedItem = item;
    }
    /// <summary>
    /// allows skin to set/get the current selected list item
    /// </summary>
    /// <value>The selected item.</value>
    public ListItem SelectedItem
    {
      get
      {
        return _selectedItem;
      }
      set
      {
        if (_selectedItem != value)
        {
          _selectedItem = value;
          UpdateContextMenu();
        }
      }
    }

    #region playlist
    /// <summary>
    /// Called by the skin to add the current item to the playlist.
    /// </summary>
    /// <param name="item">The item.</param>
    public void AddToPlayList()
    {
      MusicItem song = SelectedItem as MusicItem;
      if (song != null)
      {
        IPlaylistManager playList = ServiceScope.Get<IPlaylistManager>();
        playList.PlayList.Add(song.MediaItem);
      }
    }

    /// <summary>
    /// Called by the skin to add the current folder to the playlist.
    /// </summary>
    public void AddAllToPlayList()
    {
      foreach (ListItem item in _songs)
      {
        MusicItem song = item as MusicItem;
        if (song != null)
        {
          IPlaylistManager playList = ServiceScope.Get<IPlaylistManager>();
          playList.PlayList.Add(song.MediaItem);
        }
      }
    }
    #endregion
  }
}
