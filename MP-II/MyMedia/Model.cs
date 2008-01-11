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
using System.Diagnostics;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManager;
using MediaPortal.Core.MenuManager;
using MediaPortal.Core.Players;
using MediaPortal.Core.Settings;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Messaging;

namespace MyMedia
{
  /// <summary>
  /// Model which exposes a movie collection
  /// The movie collection are just movies & folders on the HDD
  /// </summary>
  public class Model
  {
    #region variables
    private ItemsCollection _sortMenu;
    private readonly ItemsCollection _items; //Only one items list allowed as the UI databinds to it.
    private readonly MediaSettings _settings;
    private IRootContainer currentItem = null;

    #endregion

    #region ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
      //load our settings
      _settings = new MediaSettings();
      ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      settingsManager.Load(_settings);
      _items = new ItemsCollection();
      //if (_settings.Folder != null)
        //{
      //  currentItem = MediaFactory.GetItemForFolder(_settings.Folder);
      //}
      Refresh();


      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IQueue queue = msgBroker.Get("importers");
      queue.OnMessageReceive += new MessageReceivedHandler(OnImporterMessageReceived);
    }

    void OnImporterMessageReceived(MPMessage message)
    {
      Refresh();
      _items.FireChange();
    }
    #endregion

    #region music collection methods

    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("mymedia-main"));
      }
    }

    /// <summary>
    /// provides a collection of moves to the skin
    /// </summary>
    /// <value>The movies.</value>
    public ItemsCollection Items
    {
      get
      {
        return _items;
      }
    }

    private void Refresh()
    {
      MediaFactory.LoadItems(_items, currentItem);
      //ISettingsManager settingsManager = ServiceScope.Get<ISettingsManager>();
      //if (currentItem != null)
      //  _settings.Folder = currentItem.FullPath;
      //else
      //  _settings.Folder = "";

      //settingsManager.Save(_settings);
    }


    /// <summary>
    /// provides a command for the skin to select a movie
    /// if its a folder, we build a new movie collection showing the contents of the folder
    /// if its a movie , we play it
    /// </summary>
    /// <param name="item">The item.</param>
    public void Select(ListItem item)
    {
      if (item == null)
      {
        return;
      }
      ContainerItem container = item as ContainerItem;
      if (container != null)
      {
        currentItem = container.MediaContainer;
        Refresh();
        _items.FireChange();
        return;
      }
      MediaItem mediaItem = item as MediaItem;
      if (mediaItem == null)
      {
        return;
      }
      Uri uri = mediaItem.Item.ContentUri;
      if (uri == null)
      {
        return;
      }
      ProcessItem(mediaItem);
    }

    private static void ProcessItem(MediaItem item)
    {
      IWindow window = ServiceScope.Get<IWindowManager>().CurrentWindow;
      IPlayer player;
      try
      {
        //show waitcursor
        window.WaitCursorVisible = true;

        //stop any other movies
        PlayerCollection collection = ServiceScope.Get<PlayerCollection>();
        //create a new player for our movie
        IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
        player = factory.GetPlayer(item.Item);
        if (player != null)
        {
          collection.Add(player);
          player.Play(item.Item);
          if (player.IsVideo)
          {
            IWindowManager manager = (IWindowManager)ServiceScope.Get<IWindowManager>();
            manager.ShowWindow("fullscreenvideo");
          }
        }
      }
      finally
      {
        //hide waitcursor
        window.WaitCursorVisible = false;
      }
    }

    #endregion

    #region sorting methods

    /// <summary>
    /// returns the current sort mode.
    /// </summary>
    /// <value>The sort mode.</value>
    public string SortMode
    {
      get { return _settings.SortOption.ToString(); }
    }

    /// <summary>
    /// Provides a list of sort options to the skin
    /// (used in dialogmenu.xml)
    /// </summary>
    /// <value>The sort options.</value>
    public ItemsCollection SortOptions
    {
      get
      {
        if (_sortMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _sortMenu = new ItemsCollection(menuCollect.GetMenu("mymusic-sort"));
        }

        SetSelectedSortMode();
        return _sortMenu;
      }
    }
    void SetSelectedSortMode()
    {
      for (int i = 0; i < _sortMenu.Count; ++i)
      {
        if (i != (int)_settings.SortOption)
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
          int[] values = (int[])Enum.GetValues(typeof(SortOption));
          _settings.SortOption = (SortOption)values[i];
          _items.Sort(new MediaComparer(_settings.SortOption));
          ServiceScope.Get<ISettingsManager>().Save(_settings);
        }
      }
      SetSelectedSortMode();
    }

    #endregion
  }
}
