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
using System.Net;
using System.Text;
using System.Xml;
using MediaPortal.Core.Properties;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.MediaManager;
using MediaPortal.Core.MediaManager.Views;
using MediaPortal.Core.MenuManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.WindowManager;
using MediaPortal.Core.Localisation;
using MediaPortal.Core.MPIManager;
using MediaPortal.Services.MPIManager;
using MediaPortal.Core.Logging;

namespace MyExtensions
{
  /// <summary>
  /// Model which exposes a movie collection
  /// The movie collection are just movies & folders on the HDD
  /// </summary>
  public class Model
  {
    #region variables

    private ItemsCollection _items;
    private readonly ExtensionFactory _factory;
    private ItemsCollection _sortMenu;
    private ItemsCollection _mainMenu;
    //private ItemsCollection _mainMenuInfo;
    private readonly ExtensionSettings _settings;
    private ListItem _selectedItem;
    private ListItem _selectedOtherItem;
    private ItemsCollection _viewsMenu;
    private PackageProperty _selectedPackage;

    private FolderItem _folder;
    private readonly List<IAbstractMediaItem> _extensionsViews;

    List<IMenuItem> _dynamicContextMenuItems;
    MPInstaller Installer = ServiceScope.Get<IMPInstaller>() as MPInstaller;

    enum ContextMenuItem
    {
      Install = 0,
      Unistall = 1,
      Download_And_Install = 2,
      RemoveInstall = 3,
      RemoveUninstall = 4,
      RemoveReinstall = 5,
      Reinstall = 6,
      Update = 7,
      CancelUpdate = 8,
    }
    #endregion

    #region ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
      //load our settings
      _selectedPackage = new PackageProperty();
      _factory = new ExtensionFactory();

      _settings = new ExtensionSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
      if (_settings.Folder == "")
      {
        _settings.Folder = Directory.GetCurrentDirectory();
        ServiceScope.Get<ISettingsManager>().Save(_settings);
      }
      else
      {
        _folder = _factory.GetFolder(_settings.Folder);
      }

      _items = new ItemsCollection();

      IMediaManager mediaManager = ServiceScope.Get<IMediaManager>();
      //_extensionsViews = mediaManager.GetView("/Pictures");

      _dynamicContextMenuItems = new List<IMenuItem>();
      StringId menuText = new StringId("extensions", "install");
      MenuItem menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.SafeInstall";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "uninstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.Uninstall";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "downloadinstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.DownloadInstall";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "cancelinstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.RemoveQueue";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "canceluninstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.RemoveQueue";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "cancelreinstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.RemoveQueue";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "reinstall");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.Reinstall";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "update");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.SafeUpdate";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);

      menuText = new StringId("extensions", "cancelupdate");
      menuItem = new MenuItem(menuText, "");
      menuItem.Command = "MyExtensions:Model.CancelUpdate";
      menuItem.CommandParameter = "MyExtensions:Model.SelectedItem";
      _dynamicContextMenuItems.Add(menuItem);
    }

    private void _factory_OnChanged()
    {
      _items.FireChange();
    }
    #endregion

    #region extension collection methods
    /// <summary>
    /// exposes the main menu to the skin.
    /// </summary>
    /// <value>The main menu.</value>
    public ItemsCollection MainMenu
    {
      get
      {
        if (_mainMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _mainMenu = new ItemsCollection(menuCollect.GetMenu("myextensions-main"));
        }
        return _mainMenu;
      }
    }

    public ItemsCollection DownloadListMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("myextensions-donwload-list-menu"));
      }
    }


    public ItemsCollection YesNoMenu
    {
      get
      {

        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("myextensions-yesno"));
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
        return new ItemsCollection(menuCollect.GetMenu("myextensions-contextmenu"));

      }
    }

    /// <summary>
    /// provides a collection of extensions to the skin
    /// </summary>
    /// <value>The songs.</value>
    public ItemsCollection Items
    {
      get
      {
        if (_items.Count == 0)
          _factory.LoadExtensions(ref _items, _folder);
        return _items;
      }
    }

    /// <summary>
    /// Refreshes the view.
    /// </summary>
    private void Refresh()
    {
      _factory.LoadExtensions(ref _items, _folder);
      //sort them
      _items.Sort(new ExtensionComparer(_settings.SortOption));
    }

    private void NotifyAction()
    {
      ServiceScope.Get<IWindowManager>().ShowDialog("dialogExtensionInfo");
      Refresh();
      _items.FireChange();
    }
    /// <summary>
    /// provides a command for the skin to select a extension
    /// if its a folder, we build a new collection showing the contents of the folder
    /// if its a extension , show the info screen
    /// </summary>
    /// <param name="item">The item.</param>
    public void Select(ListItem item)
    {
      if (item == null)
      {
        return;
      }
      //did user select a folder ?
      if ((item as FolderItem) != null)
      {
        // yes then load the folder and return its items
        _folder = (FolderItem)item;
        Refresh();
        _items.FireChange();
        return;
      }
      else
      {

        if (((ExtensionItem)item).Item.Dependencies.Count == 0 &&((ExtensionItem)item).Item.Items.Count == 0)
          _factory.DownloadExtraInfo(((ExtensionItem)item).Item);
        ((ExtensionItem)item).Item = Installer.Enumerator.GetItem(((ExtensionItem)item).Item.PackageId);

        _selectedPackage.Set(((ExtensionItem)item).Item);
        ServiceScope.Get<IWindowManager>().ShowWindow("extensionsInfo");
      }
    }

    public void OnSelectionChange(ListItem item)
    {
      SelectedItem = item;
    }

    #endregion

    #region sorting methods

    /// <summary>
    /// Exposes the current sort mode to the skin
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
          _sortMenu = new ItemsCollection(menuCollect.GetMenu("myextensions-sort"));
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
          Refresh();
          _items.FireChange();
          ServiceScope.Get<ISettingsManager>().Save(_settings);
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
          //add our own shares-view
          ItemsCollection items = new ItemsCollection();
          _viewsMenu.Add(new ListItem("Name", new StringId("extensions", "collection")));
          _viewsMenu.Add(new ListItem("Name", new StringId("extensions", "myextensions")));
          _viewsMenu.Add(new ListItem("Name", new StringId("extensions", "flatview")));
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
      for (int i = 0; i < _extensionsViews.Count; ++i)
      {
        if (_extensionsViews[i].FullPath == _folder.MediaContainer.FullPath)
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
      for (int i = 0; i < _viewsMenu.Count; i++)
      {
        if (_viewsMenu[i].Labels["Name"].Evaluate(null, null) == selectedItem.Labels["Name"].Evaluate(null, null))
        {
          _factory._view = i;
          _settings.View = i;
          break;
        }
      }
      Refresh();
      _items.FireChange();
      ServiceScope.Get<ISettingsManager>().Save(_settings);
    }

    #endregion

    #region install/uninstall
    
    public void SafeInstall(ListItem selectedItem)
    {
      ServiceScope.Get<IWindowManager>().CloseDialog();
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item.Item.Dependencies.Count == 0 && item.Item.Items.Count == 0)
        _factory.DownloadExtraInfo(item.Item);
      item.Item = Installer.Enumerator.GetItem(item.Item.PackageId);
      if (Installer.GetUnsolvedDependencies(item.Item).Count > 0)
      {
        ServiceScope.Get<IWindowManager>().ShowDialog("dialogExtensionDependency");
      }
      else
      {
        Install(selectedItem);
      }
    }
    
    public void SafeUpdate(ListItem selectedItem)
    {
      ServiceScope.Get<IWindowManager>().CloseDialog();
      ExtensionItem item = selectedItem as ExtensionItem;
      item.Item = Installer.Enumerator.GetItem(item.Item.PackageId);

      MPIEnumeratorObject latestObj = Installer.Enumerator.GetExtensions(item.Item.ExtensionId);
      if (latestObj.Dependencies.Count == 0 && latestObj.Items.Count == 0)
        _factory.DownloadExtraInfo(latestObj);
      latestObj = Installer.Enumerator.GetExtensions(item.Item.ExtensionId);
      Installer.AddToQueue(item.Item as IMPIPackage, "Uninstall");

      foreach (MPIEnumeratorObject obj in Installer.GetUnsolvedDependencies(item.Item))
      {
        Installer.AddToQueue(obj, "Install");
      }
      Installer.AddToQueue(latestObj as IMPIPackage, "Install");
      
      UpdateContextMenu();
      ContextMenu.FireChange(true);
      NotifyAction();
    }
    /// <summary>
    /// Called by the skin if the user wants to add the current selected item as a share
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void Install(ListItem selectedItem)
    {
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        foreach(MPIEnumeratorObject obj in Installer.GetUnsolvedDependencies(item.Item))
        {
          Installer.AddToQueue(obj, "Install");
        }
        Installer.AddToQueue(item.Item as IMPIPackage, "Install");
      }
      UpdateContextMenu();
      ContextMenu.FireChange(true);
      NotifyAction();
    }


    /// <summary>
    /// Uninstalls the specified selected item.
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void Uninstall(ListItem selectedItem)
    {
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        Installer.AddToQueue(item.Item as IMPIPackage, "Uninstall");
      }
      UpdateContextMenu();
      ContextMenu.FireChange(true);
      NotifyAction();
    }

    /// <summary>
    /// Reinstalls the specified selected item.
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void Reinstall(ListItem selectedItem)
    {
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        Installer.AddToQueue(item.Item as IMPIPackage, "Uninstall");
        Installer.AddToQueue(item.Item as IMPIPackage, "Install");
      }
      UpdateContextMenu();
      ContextMenu.FireChange(true);
      NotifyAction();
    }

    /// <summary>
    /// Install not locally stored package.
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void DownloadInstall(ListItem selectedItem)
    {
      ServiceScope.Get<IWindowManager>().CloseDialog();
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        string tempFile=Path.GetTempFileName();
        //if (_factory.DownloadFile(item.Item.DownloadUrl, tempFile))
        //{
          //IMPIPackage pak = Installer.LoadPackageFromMPI(tempFile);
          //if (pak != null)
            //Installer.AddToQueue(pak, "Install");
        //}
        Installer.AddToQueue((IMPIPackage)item.Item, "Install");
      }
      UpdateContextMenu();
      NotifyAction();
    }

    /// <summary>
    /// Removes from queue.
    /// </summary>
    /// <param name="selectedItem">The selected item.</param>
    public void RemoveQueue(ListItem selectedItem)
    {
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        Installer.RemoveFromQueue(item.Item.PackageId);
      }
      Refresh();
      _items.FireChange();
      UpdateContextMenu();
    }

    public void CancelUpdate(ListItem selectedItem)
    {
      ExtensionItem item = selectedItem as ExtensionItem;
      if (item != null)
      {
        Installer.RemoveAllFromQueue(item.Item.ExtensionId);
      }
      Refresh();
      _items.FireChange();
      UpdateContextMenu();
    }

    #endregion

    /// <summary>
    /// updates the context menu depending on what item is currently selected
    /// </summary>
    void UpdateContextMenu()
    {
      IMenu menu = ServiceScope.Get<IMenuCollection>().GetMenu("myextensions-contextmenu");
      foreach (IMenuItem menuItem in _dynamicContextMenuItems)
      {
        menu.Items.Remove(menuItem);
      }
      if (SelectedItem != null)
      {
        //menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.AddAllToPlaylist]);
        if ((_selectedItem as FolderItem) == null)
        {
          ExtensionItem item = SelectedItem as ExtensionItem;
          if (item != null)
          {
            MPIQueueObject queueItem = Installer.GetQueueItem(item.Item.PackageId);
            if (queueItem != null)
            {
              List <MPIQueueObject> queueObjs= Installer.GetQueueItems(item.Item.ExtensionId);
              if (queueObjs.Count > 1 && queueObjs[0].PackageId != queueObjs[1].PackageId)
              {
                menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.CancelUpdate]);
              }
              else if (queueObjs.Count > 1 && queueObjs[0].PackageId == queueObjs[1].PackageId)
              {
                menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.RemoveReinstall]);
              }
              else
              {
                if (queueItem.Action == "Install")
                  menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.RemoveInstall]);
                else
                  menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.RemoveUninstall]);
              }
            }
            else
            {
              if (item.Item.State == MediaPortal.Services.MPIManager.MPIPackageState.Installed)
              {
                menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.Unistall]);
                menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.Reinstall]);
                if (Installer.Enumerator.HaveUpdate(item.Item))
                  menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.Update]);
              }
              else 
                menu.Items.Add(_dynamicContextMenuItems[(int)ContextMenuItem.Install]);
            }
          }
        }
      }
    }

    /// <summary>
    /// Downloads the OpenMaid list.
    /// </summary>
    public void DownloadList()
    {
      Directory.CreateDirectory(String.Format(@"{0}\MediaPortal\Installer", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
      IWindow window = ServiceScope.Get<IWindowManager>().CurrentWindow;
      string url = String.Format("http://openmaid.team-mediaportal.com/xtern.php?sync");
      string listFile = String.Format(@"{0}\MediaPortal\Installer\Mpilist.xml", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
      if (_factory.DownloadFile(url, listFile))
      {
        Installer.Enumerator.UpdateList(listFile);
        Installer.Enumerator.Save();
      }
      Refresh();
      _items.FireChange();
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
    
    public ListItem SelectedOtherItem
    {
      get
      {
        return _selectedOtherItem;
      }
      set
      {
        if (_selectedOtherItem != value)
        {
          _selectedOtherItem = value;
        }
      }
    }
    #region info screen

    public PackageProperty SelectedPackage
    {
      get
      {
        return _selectedPackage;
      }
      set
      {
        if (_selectedPackage != value)
        {
          _selectedPackage = value;
        }
      }
    }


    public ItemsCollection MainMenuInfo
    {
      get
      {
        //if (_mainMenuInfo == null)
        //{
        //  IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        //  _mainMenuInfo = new ItemsCollection(menuCollect.GetMenu("myextensions-info"));
        //}
        return ContextMenu;
      }
    }
    public string LongNameProperty
    {
      get
      {
        return "Test";
      }
  
    }
    #endregion
  }
}
