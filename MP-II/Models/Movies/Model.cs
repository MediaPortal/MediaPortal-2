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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.Players;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManager;
using MediaPortal.Presentation.Screen;

namespace Models.Movies
{
  /// <summary>
  /// Model which exposes a movie collection
  /// The movie collection are just movies & folders on the HDD
  /// </summary>
  public class Model
  {
    public const string IMDBIMPORTERSQUEUE_NAME = "IMDBImporters";
    public const string IMPORTERSQUEUE_NAME = "Importers";
    public const string MEDIAMANAGERQUEUE_NAME = "MediaManager";

    #region Imports

    [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
    protected static extern int mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength,
                                              IntPtr hwndCallback);
    #endregion

    #region variables

    private ItemsCollection _mainMenu;
    private ItemsCollection _sortMenu;
    private ItemsCollection _viewsMenu;
    private ItemsCollection _movies;

    private MovieFactory _factory;
    private MovieSettings _settings;
    private FolderItem _folder;
    private List<IAbstractMediaItem> _movieViews;
    ListItem _selectedItem;

    List<IMenuItem> _dynamicContextMenuItems;
    IMetaDataMappingCollection _currentMap;

    enum ContextMenuItem
    {
      AddShare = 0,
      RemoveShare = 1,
      ForceImport = 2,
    }
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
      _movies = new ItemsCollection();
      _factory = new MovieFactory();

      //get settings
      _settings = new MovieSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);

      // get movie-views
      SetView("Movies");

      if (_settings.Folder == "")
      {
        SelectView(Views[0]);
      }
      else
      {
        _factory.LoadMovies(ref _movies, ref _currentMap, _settings.Sort, _settings.Folder);
        if (_movies.Count == 0)
        {
          SelectView(Views[0]);
        }
      }
      if (_movies.Count > 0)
      {
        FolderItem f = _movies[_movies.Count - 1] as FolderItem;
        MovieItem p = _movies[_movies.Count - 1] as MovieItem;
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
      IMessageQueue queue = broker.GetOrCreate("players");
      queue.OnMessageReceive += new MessageReceivedHandler(OnPlayerMessageReceived);
      queue = broker.GetOrCreate(MEDIAMANAGERQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnMediaManagerMessageReceived);

      queue = broker.GetOrCreate(IMPORTERSQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnImporterMessageReceived);

      queue = broker.GetOrCreate(IMDBIMPORTERSQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnImdbImporterMessageReceived);

      //create our dynamic context menu items
      _dynamicContextMenuItems = new List<IMenuItem>();
    }

    /// <summary>
    /// Called when [importer message received].
    /// </summary>
    /// <param name="message">The message.</param>
    void OnImporterMessageReceived(QueueMessage message)
    {
      string mes = message.MessageData["action"] as string;
      switch (mes)
      {
        case "fileinfoupdated":
          ServiceScope.Get<ILogger>().Debug("File info update message received: {0}", message.MessageData["file"] as string);
          break;
        default:
          break;
      }
      Refresh();
      _movies.FireChange();
    }

    public void SetView(string viewName)
    {
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      _movieViews = mediaManager.GetView("/" + viewName);
      _currentMap = _movieViews[0].Mapping;
      _folder = null;

      _factory.LoadMovies(ref _movies, ref _currentMap, _settings.Sort, _movieViews[0].FullPath);
      if (_movies.Count > 0)
      {
        _movies.Sort(new MovieComparer(_settings.Sort, _currentMap));

        FolderItem f = _movies[_movies.Count - 1] as FolderItem;
        MovieItem p = _movies[_movies.Count - 1] as MovieItem;
        if (f != null)
        {
          _folder = new FolderItem(f.MediaContainer.Parent);
        }
        if (p != null)
        {
          _folder = new FolderItem(p.MediaItem.Parent);
        }
      }
      _movies.FireChange();
    }

    #region menus

    /// <summary>
    /// exposes the movie-ended menu to the skin
    /// </summary>
    /// <value>The movie ended menu.</value>
    public ItemsCollection MovieEndedMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("mymovies-playback-ended"));
      }
    }

    /// <summary>
    /// exposes the movie-stopped menu to the skin
    /// </summary>
    /// <value>The movie stopped menu.</value>
    public ItemsCollection MovieStoppedMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("mymovies-playback-stopped"));
      }
    }

    /// <summary>
    /// exposes the movie-resume menu to the skin
    /// </summary>
    /// <value>The movie resume menu.</value>
    public ItemsCollection MovieResumeMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("mymovies-playback-resume"));
      }
    }
    /// <summary>
    /// exposes the main menu to the skin
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        _mainMenu = new ItemsCollection(menuCollect.GetMenu("mymovies-main"));

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
        return new ItemsCollection(menuCollect.GetMenu("mymovies-contextmenu"));
      }
    }

    /// <summary>
    /// updates the context menu.
    /// Depending on what media item is currently selected
    /// we add one or more of our dynamic menu items to the context menu
    /// </summary>
    void UpdateContextMenu()
    {
      IMenu menu = ServiceScope.Get<IMenuCollection>().GetMenu("mymovies-contextmenu");
      foreach (IMenuItem menuItem in _dynamicContextMenuItems)
      {
        menu.Items.Remove(menuItem);
      }


    }
    #endregion

    #region movie-collection
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
                _movies.FireChange();
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// provides a collection of movies to the skin
    /// </summary>
    /// <value>The movies.</value>
    public ItemsCollection Movies
    {
      get
      {
        IImporterManager importer = ServiceScope.Get<IImporterManager>();
        if (importer.Shares.Count == 0)
        {
          //ServiceScope.Get<IScreenManager>().ShowDialog("dialogNoSharesDefined");
          Refresh();
        }
        return _movies;
      }
    }

    /// <summary>
    /// reloads the movie collection for the current folder.
    /// </summary>
    private void Refresh()
    {
      //load movie collection for current folder
      _factory.LoadMovies(ref _movies, _folder, ref _currentMap, _settings.Sort);

      //sort it
      _movies.Sort(new MovieComparer(_settings.Sort, _currentMap));
    }

    #endregion

    #region methods which can be called from the skin

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

    /// <summary>
    /// Expose playdvd command to skin
    /// Plays the DVD.
    /// </summary>
    public void PlayDvd()
    {
      try
      {
        //window.WaitCursorVisible = true;
        IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
        IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
        IMediaItem mediaItem = new DvdMediaItem();
        mediaItem.MetaData["MyMovies.Model"] = true;
        IPlayer player = factory.GetPlayer(mediaItem);

        //play it
        player.Play(mediaItem);
        collection.Add(player);
        if (player.CanResumeSession(null))
        {
          player.Paused = true;
          ServiceScope.Get<IScreenManager>().ShowDialog("movieResume");
        }

      }
      finally
      {
        //window.WaitCursorVisible = false;
      }
      IScreenManager manager = (IScreenManager)ServiceScope.Get<IScreenManager>();
      manager.ShowScreen("fullscreenvideo");
    }

    /// <summary>
    /// Expose Eject command to skin
    /// Ejects the cd/dvd from the drive
    /// </summary>
    public void Eject()
    {
      mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
    }

    /// <summary>
    /// provides a command for the skin to select a movie or folder
    /// if its a folder, we build a new movie collection showing the contents of the folder
    /// if its a movie , we play it
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
        ServiceScope.Get<ISettingsManager>().Load(_settings);
        if (_folder.MediaContainer != null)
          _settings.Folder = _folder.MediaContainer.FullPath;
        else
          _settings.Folder = "";
        ServiceScope.Get<ISettingsManager>().Save(_settings);
        Refresh();
        _movies.FireChange();
        return;
      }
      else
      {
        //no user clicked on a media item
        MovieItem movie = (MovieItem)item;
        Uri uri = movie.MediaItem.ContentUri;
        if (uri == null)
        {
          return; // no uri? then return
        }
        //seems this is a movie, lets play it
        try
        {
          //show waitcursor
          //window.WaitCursorVisible = true;

          //stop any other movies
          IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
          //create a new player for our movie
          IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
          IPlayer player = factory.GetPlayer(movie.MediaItem);

          if (collection.Count > 0 && collection[0].IsAudio)
          {
            collection.Dispose();
          }
          //add it to the player collection
          collection.Add(player);

          //play it
          player.Play(movie.MediaItem);

          if (player.CanResumeSession(uri))
          {
            player.Paused = true;
            ServiceScope.Get<IScreenManager>().ShowDialog("movieResume");
          }

        }
        finally
        {
          //hide waitcursor
          //window.WaitCursorVisible = false;
        }

        // show fullscreen video window
        IScreenManager manager = (IScreenManager)ServiceScope.Get<IScreenManager>();
        manager.ShowScreen("fullscreenvideo");

      }
    }


    #endregion

    #region sorting methods

    /// <summary>
    /// Provides a list of sort options to the skin
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
    /// <param name="item">The item.</param>
    public void Sort(ListItem selectedItem)
    {

      for (int i = 0; i < _sortMenu.Count; ++i)
      {
        if (selectedItem == _sortMenu[i])
        {
          _settings.Sort = i;
          // _movies.Sort(new MovieComparer(_settings.SortOption));
          ServiceScope.Get<ISettingsManager>().Save(_settings);
          Refresh();
          _movies.FireChange();
        }
      }
      SetSelectedSortMode();
    }

    #endregion

    #region view methods

    /// <summary>
    /// returns all views
    /// </summary>
    /// <value>The views.</value>
    public ItemsCollection Views
    {
      get
      {
        if (_viewsMenu == null)
        {
          _viewsMenu = new ItemsCollection();
          foreach (IAbstractMediaItem view in _movieViews)
          {
            _viewsMenu.Add(new ListItem("Name", view.Title));
          }
        }
        SetSelectedView();

        return _viewsMenu;
      }
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

    void SetSelectedView()
    {
      if (_folder == null) return;
      if (_folder.MediaContainer == null) return;
      for (int i = 0; i < _viewsMenu.Count; ++i)
      {
        _viewsMenu[i].Selected = false;
      }
      for (int i = 0; i < _movieViews.Count; ++i)
      {
        if (_movieViews[i].FullPath == _folder.MediaContainer.FullPath)
        {
          _viewsMenu[i].Selected = true;
        }
      }
    }

    /// <summary>
    /// Selects a view.
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void SelectView(ListItem selectedItem)
    {
      if (selectedItem == null) return;
      foreach (IAbstractMediaItem item in _movieViews)
      {
        if (item.Title == selectedItem.Labels["Name"].Evaluate())
        {
          SetSelectedView();
          _settings.Folder = item.FullPath;
          ServiceScope.Get<ISettingsManager>().Save(_settings);

          ListItem selectedView = Views[SelectedViewIndex];
          string viewName = selectedView.ToString();
          _factory.LoadMovies(ref _movies, ref _currentMap, _settings.Sort, item.FullPath);
          _movies.Sort(new MovieComparer(_settings.Sort, _currentMap));
          _movies.FireChange();
          return;
        }
      }
    }

    #endregion


    /// <summary>
    /// Method which gets called when a new player related message has been received
    /// </summary>
    /// <param name="message">The message.</param>
    void OnPlayerMessageReceived(QueueMessage message)
    {
      IPlayerCollection collection = ServiceScope.Get<IPlayerCollection>();
      IPlayer player = message.MessageData["player"] as IPlayer;
      string action = message.MessageData["action"] as string;
      if (player != null && player.IsVideo)
      {
        IMediaItem mediaItem = player.MediaItem;
        if (collection.Count > 0)
        {
          if (collection[0] == player)
          {
            if (action == "stopped")
            {
              ServiceScope.Get<IScreenManager>().ShowScreen("movieStopped");
              message.MessageData["handled"] = "true";
            }
            if (action == "ended")
            {
              ServiceScope.Get<IScreenManager>().ShowScreen("movieEnded");
              message.MessageData["handled"] = "true";
            }
          }

        }
      }
    }

    public void StopAndDeleteMovie()
    {
      IPlayerCollection players = ServiceScope.Get<IPlayerCollection>();
      if (players.Count > 0)
      {
        IPlayer player = players[0];
        Uri filename = player.FileName;
        players.Dispose();
        if (filename.IsFile)
        {
          try
          {
            File.Delete(filename.LocalPath);
          }
          catch (Exception) { }
        }
      }
    }
   
    
    public bool CanResume(ListItem item)
    {
      return true;
    }

    #region ImdbStuffs

    private ItemsCollection _imdbresultmovies = new ItemsCollection();
    private Queue<QueueMessage> _imdbmessages = new Queue<QueueMessage>();
    private Property _imdbmovietile=new Property(typeof(string));

    /// <summary>
    /// Gets the imdb movietile.
    /// </summary>
    /// <value>The imdb movietile.</value>
    public Property ImdbMovietile
    {
      get
      {
        return _imdbmovietile;
      }
    }

    /// <summary>
    /// Gets the imdb result movies.
    /// </summary>
    /// <value>The imdb result movies.</value>
    public ItemsCollection ImdbResultMovies
    {
      get
      {
        return _imdbresultmovies;
      }
    }
    
    /// <summary>
    /// Called when [imdb importer message received].
    /// </summary>
    /// <param name="message">The message.</param>
    void OnImdbImporterMessageReceived(QueueMessage message)
    {
      string mes = message.MessageData["action"] as string;
      switch (mes)
      {
        case "fileinfoupdated":
          ServiceScope.Get<ILogger>().Debug("File info update message received: {0}", message.MessageData["file"] as string);
          break;
        case "imdbchoiceneeded":
          ShowImdbChoice(message);
          break;
        default:
          break;
      }
      Refresh();
      _movies.FireChange();
    }

    void ShowImdbChoice(QueueMessage msg)
    {
      _imdbresultmovies.Clear();
      foreach (string title in msg.MessageData["titles"] as List<string>)
      {
        ListItem item = new ListItem();
        item.Add("Name",title);
        _imdbresultmovies.Add(item);
      }
      _imdbresultmovies.FireChange();
      _imdbmovietile.SetValue(msg.MessageData["title"] as string);
      ServiceScope.Get<IScreenManager>().ShowScreen("imdbresultchoice");
    }

    #endregion
  }
}
