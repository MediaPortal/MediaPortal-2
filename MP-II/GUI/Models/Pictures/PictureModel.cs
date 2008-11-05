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
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MetaData;
using MediaPortal.Media.MediaManagement;
using MediaPortal.Presentation.Screen;


namespace Models.Pictures
{
  /// <summary>
  /// Model which exposes a pictures collection
  /// The movie collection are just pictures & folders on the HDD
  /// </summary>
  public class PictureModel
  {
    public const string IMPORTERSQUEUE_NAME = "Importers";
    public const string PICTUREVIEWERQUEUE_NAME = "PictureViewer";

    #region imports

    [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
    protected static extern int mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength,
                                              IntPtr hwndCallback);

    #endregion

    #region variables

    private ItemsCollection _mainMenu;
    private ItemsCollection _sortMenu;
    private ItemsCollection _viewsMenu;
    private ItemsCollection _pictures;
    private PictureSettings _settings;
    private readonly PictureFactory _factory;

    private FolderItem _folder;
    private readonly IList<IAbstractMediaItem> _pictureViews;
    ListItem _selectedItem;
    IMetaDataMappingCollection _currentMap;
    SlideShow _slideShow;
    PictureEditor _editor;
    IList<IMenuItem> _dynamicContextMenuItems;
    enum ContextMenuItem
    {
      AddShare = 0,
      RemoveShare = 1,
      ForceImport = 2,
    }


    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="PictureModel"/> class.
    /// </summary>
    public PictureModel()
    {
      //load our settings
      _settings = new PictureSettings();
      _factory = new PictureFactory();
      _pictures = new ItemsCollection();

      // get picture-views
      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      _pictureViews = mediaManager.GetView("/Pictures");
      _currentMap = _pictureViews[0].Mapping;

      _settings = ServiceScope.Get<ISettingsManager>().Load<PictureSettings>();

      //get settings
      _settings = ServiceScope.Get<ISettingsManager>().Load<PictureSettings>();
      if (_settings.Folder == "")
      {
        SelectView(Views[0]);
      }
      else
      {
        _factory.LoadPictures(ref _pictures, ref _currentMap, _settings.Sort, _settings.Folder);
        if (_pictures.Count == 0)
        {
          SelectView(Views[0]);
        }
      }
      if (_pictures.Count > 0)
      {
        FolderItem f = _pictures[_pictures.Count-1] as FolderItem;
        PictureItem p = _pictures[_pictures.Count - 1] as PictureItem;
        if (f!=null) 
        {
          _folder=new FolderItem(f.MediaContainer.Parent);
        }
        if (p!=null) 
        {
          _folder=new FolderItem(p.MediaItem.Parent);
        }
      }
      _slideShow = new SlideShow(ref _pictures);
      _editor = new PictureEditor(_slideShow);

      //create our dynamic context menu items
      _dynamicContextMenuItems = new List<IMenuItem>();


      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate(PICTUREVIEWERQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(queue_OnMessageReceive);

      queue = msgBroker.GetOrCreate(IMPORTERSQUEUE_NAME);
      queue.OnMessageReceive += new MessageReceivedHandler(OnImporterMessageReceived);

    }

    void OnImporterMessageReceived(QueueMessage message)
    {
      Refresh();
      _pictures.FireChange();
    }

    void queue_OnMessageReceive(QueueMessage message)
    {
      if (message.MessageData.ContainsKey("action"))
      {
        if (message.MessageData["action"].ToString() == "show")
        {
          if (message.MessageData.ContainsKey("mediaitem"))
          {
            IMediaItem mediaItem= message.MessageData["mediaitem"] as IMediaItem;
            if (mediaItem != null)
            {
              _pictures.Clear();
              _pictures.Add(new PictureItem(mediaItem));
              _slideShow.PictureIndex=0;
              _slideShow.UpdateCurrentPicture();
            }
          }
        }
      }
    }

    #region Picture collection methods
    public PictureEditor Editor
    {
      get
      {
        return _editor;
      }
    }

    public ItemsCollection EditMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("mypictures-editting"));
      }
    }

    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        _mainMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("mypictures-main"));

        return _mainMenu;
      }
    }
    /// <summary>
    /// provides a collection of pictures to the skin
    /// </summary>
    /// <value>The Pictures.</value>
    public ItemsCollection Pictures
    {
      get
      {
        // FIXME: Do not open a dialog during the evaluation of a property, this has to be
        // solved in another way. Same in Music.Model class.
        //IImporterManager importer = ServiceScope.Get<IImporterManager>();
        //if (importer.Shares.Count == 0)
        //{
        //  ServiceScope.Get<IScreenManager>().ShowDialog("dialogNoSharesDefined");
        //  Refresh();
        //}
        return _pictures;
      }
    }

    public SlideShow SlideShow
    {
      get
      {
        return _slideShow;
      }
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
                _pictures.FireChange();
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Refreshes the view.
    /// </summary>
    private void Refresh()
    {
      _factory.LoadPictures(ref _pictures, _folder, ref _currentMap, _settings.Sort);

      //sort it
      _pictures.Sort(new PictureComparer(_settings.Sort, _currentMap));
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
    /// provides a command for the skin to select a movie
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
        _settings = ServiceScope.Get<ISettingsManager>().Load<PictureSettings>();
        if (_folder.MediaContainer != null)
          _settings.Folder = _folder.MediaContainer.FullPath;
        else
          _settings.Folder = "";
        ServiceScope.Get<ISettingsManager>().Save(_settings);
        Refresh();
        _pictures.FireChange();
        return;
      }
      else
      {
        //no user clicked on a media item
        PictureItem picture = (PictureItem)item;
        Uri uri = picture.MediaItem.ContentUri;
        if (uri == null)
        {
          //if item.Labels
          return; // no uri? then return
        }
        for (int i = 0; i < _pictures.Count; ++i)
        {
          if (_pictures[i] == item)
          {
            _slideShow.PictureIndex = i;
            break;
          }
        }
        //we dont handle .jpg or .png (yet)
        //if (uri.AbsoluteUri.IndexOf(".jpg") < 0 && uri.AbsoluteUri.IndexOf(".png") < 0)
        {
          //seems this is a picture, lets play it
          try
          {
            //show waitcursor
            //window.WaitCursorVisible = true;

            //play it
            SlideShow.CurrentPictureUri = uri;
            SlideShow.CurrentPicture = uri.AbsoluteUri;
            SlideShow.CurrentTitle = item.Labels["Name"].Evaluate();
            //add it to the player collection
          }
          finally
          {
            //hide waitcursor
            //window.WaitCursorVisible = false;
          }

          // show fullscreen video window
          IScreenManager manager = ServiceScope.Get<IScreenManager>();
          manager.ShowScreen("pictureviewer");
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
          _pictures.FireChange();
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
          foreach (IAbstractMediaItem view in _pictureViews)
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
      for (int i = 0; i < _pictureViews.Count; ++i)
      {
        if (_pictureViews[i].FullPath == _folder.MediaContainer.FullPath)
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
      foreach (IAbstractMediaItem item in _pictureViews)
      {
        if (item.Title == selectedItem.Labels["Name"].Evaluate())
        {
          SetSelectedView();
          _settings.Folder = item.FullPath;
          ServiceScope.Get<ISettingsManager>().Save(_settings);

          ListItem selectedView = Views[SelectedViewIndex];
          string viewName = selectedView.ToString();
          _factory.LoadPictures(ref _pictures, ref _currentMap, _settings.Sort, item.FullPath);
          _pictures.Sort(new PictureComparer(_settings.Sort, _currentMap));
          _pictures.FireChange();
          return;
        }
      }
    }


    /// <summary>
    /// updates the context menu.
    /// Depending on what media item is currently selected
    /// we add one or more of our dynamic menu items to the context menu
    /// </summary>
    void UpdateContextMenu()
    {
      IMenu menu = ServiceScope.Get<IMenuCollection>().GetMenu("mypictures-context");
      foreach (IMenuItem menuItem in _dynamicContextMenuItems)
      {
        menu.Items.Remove(menuItem);
      }

      //if (SelectedItem != null)
      //{
      //  if ((SelectedItem as FolderItem) != null)
      //  {
      //    FolderItem folder = (FolderItem)SelectedItem;
      //    if (folder.MediaContainer != null && folder.MediaContainer.ContentUri != null)
      //    {
      //      if (Directory.Exists(folder.MediaContainer.ContentUri.LocalPath))
      //      {
      //        IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      //        if (mgr.Shares.Contains(folder.MediaContainer.ContentUri.LocalPath))
      //        {
      //          menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.ForceImport]);
      //          menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.RemoveShare]);
      //        }
      //        else
      //        {
      //          menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.AddShare]);
      //        }
      //      }
      //    }
      //  }
      //}
    }


    /// <summary>
    /// returns context menu options for picture
    /// </summary>
    /// <value>The context menu options.</value>
    public ItemsCollection ContextMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("mypictures-context"));
      }
    }

    #endregion

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
          for (int i = 0; i < _pictures.Count; ++i)
          {
            if (_pictures[i] == _selectedItem)
            {
              _slideShow.PictureIndex = i;
              break;
            }
          }
        }
      }
    }
  }
}
