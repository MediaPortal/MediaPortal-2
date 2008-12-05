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
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Media.Importers;
using MediaPortal.Presentation.Screen;

namespace Models.Shares
{
  /// <summary>
  /// Model which holds the GUI state for the shares configuration screens.
  /// </summary>
  public class Model
  {
    /// <summary>
    /// Holds a cached list of all shares available in the <see cref="MediaManager"/>.
    /// For GUI presentation.
    /// </summary>
    protected ItemsList _shares = new ItemsList();

    /// <summary>
    /// Holds a cached list of all local media providers available in the <see cref="MediaManager"/>.
    /// For GUI presentation.
    /// </summary>
    protected ItemsList _mediaProviders = new ItemsList();

    /// <summary>
    /// Holds a cached tree of all directories provided by the choosen media provider.
    /// For GUI presentation.
    /// </summary>
    protected ItemsList _providerDirectories = new ItemsList();

    /// <summary>
    /// Holds a list of all categories to be choosen from.
    /// For GUI presentation.
    /// </summary>
    protected ItemsList _mediaCategories = new ItemsList();

    /// <summary>
    /// Holds a cached list of all metadata extractors available in the <see cref="MediaManager"/>.
    /// For GUI presentation.
    /// </summary>
    protected ItemsList _metadataExtractors = new ItemsList();

    /// <summary>
    /// Holds the media provider for the new share.
    /// Input from GUI.
    /// </summary>
    protected Guid _mediaProvider = null;

    /// <summary>
    /// Holds the path for the new share.
    /// Input from GUI.
    /// </summary>
    protected string _path = null;

    /// <summary>
    /// Holds the name for the new share.
    /// Input from GUI.
    /// </summary>
    protected string _name = null;

    public Model()
    {
      UpdateGuiData();
    }

    public ItemsList Shares
    {
      get { return _shares; }
    }

    public void RemoveShare(ListItem item)
    {
      if (item == null) return;
      string path = item.Labels["Path"].Evaluate();
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      mgr.RemoveShare(path);
      UpdateShares();
    }

    public ItemsList NoSharesMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("shares-notdefined-main"));
      }
    }

    public ItemsList SharesRemoveMainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("shares-remove-main"));
      }
    }

    public ItemsList SharesAddMainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("shares-add-main"));
      }
    }

    public ItemsList MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return MenuHelper.WrapMenu(menuCollect.GetMenu("shares-main"));
      }
    }
    
    /// <summary>
    /// Returns the local shares collection. This is a copy of all shares registered in the
    /// importer manager. The local shares collection can be updated from the importer manager by calling
    /// <see cref="UpdateShares"/>, and can be written to the importer manager by calling
    /// <see cref="CommitShares"/>.
    /// </summary>
    public ItemsList Folders
    {
      get { return _folders; }
    }

    /// <summary>
    /// Copies the shares from the importer manager to our local shares collection.
    /// </summary>
    public void UpdateShares()
    {
      _shares.Clear();
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      ICollection<string> shares = new List<string>();
      foreach (string share in mgr.Shares)
      {
        FolderItem item = new FolderItem(Path.GetFileName(share), share, null);
        _shares.Add(item);
        shares.Add(share);
      }
      _shares.FireChange();
      SynchronizeShares();
    }

    public void CommitShares()
    {
      ICollection<string> localShares = new List<string>();
      foreach (FolderItem localShare in _shares)
        localShares.Add(localShare.Folder);
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      ICollection<string> mgrShares = mgr.Shares;
      foreach (string share in mgrShares)
      {
        if (!localShares.Remove(share))
          // Share is not present in local shares collection
          mgr.RemoveShare(share);
      }
      foreach (string localShare in localShares)
        mgr.AddShare(localShare);
    }

    /// <summary>
    /// Will add or remove the specified <paramref name="folder"/> item to or from the shares manager's
    /// collection.
    /// </summary>
    /// <param name="folder">The folder which should be selected or deselected in the
    /// medialibrary.</param>
    /// <param name="value">If set to <c>true</c>, the selection will be set, else it will be reset.</param>
    public void SetSelection(FolderItem folder, bool value)
    {
      if (folder == null) return;
      folder.Selected = value; // Update UI
      // Update local shares
      if (value)
        AddLocalShare(folder);
      else
        RemoveLocalShare(folder);
      // Update UI
      folder.FireChange();
    }

    public void Save()
    {
      CommitShares();
      UpdateShares();
      ServiceScope.Get<IScreenManager>().ShowPreviousScreen();
    }

    /// <summary>
    /// Refreshes the subitems of the specified <paramref name="listItem"/> or clears them.
    /// The specified <paramref name="listItem"/> must be one of the items returned by the
    /// <see cref="Folders"/> property, or any subitem.
    /// This method can be called from skin to populate the subitems of the specified list item.
    /// </summary>
    /// <param name="listItem">Item containing the information about one folder.</param>
    /// <param name="clear">If set to <c>true</c>, this method will clear the subitems of
    /// the specified <paramref name="listItem"/>. This can be done if the tree node of this item
    /// is closed. If set to <c>false</c>, the method will populate the subitems.</param>
    public void RefreshOrClearSubItems(ListItem listItem, bool clear)
    {
      FolderItem folderItem = listItem as FolderItem;
      if (folderItem == null) return;
      if (clear)
      {
        folderItem.SubItems.Clear();
        folderItem.SubItems.FireChange();
      }
      else
        Refresh(folderItem.SubItems, folderItem, false);
    }

    /// <summary>
    /// Starts a database refresh for all defined shares
    /// </summary>
    public void RefreshLibrary()
    {
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (string share in mgr.Shares)
      {
        mgr.ForceImport(share, true);
      }
    }

    /// <summary>
    /// Updates the folders tree with the current local shares collection.
    /// </summary>
    protected void SynchronizeShares()
    {
      // Build shares "index"
      ICollection<string> shares = new List<string>();
      foreach (FolderItem share in _shares)
        shares.Add(share.Folder);
      SynchronizeShares(_folders, shares);
    }

    protected void SynchronizeShares(ItemsList list, ICollection<string> shares)
    {
      foreach (FolderItem folder in list)
      {
        folder.Selected = shares.Contains(folder.Folder);
        SynchronizeShares(folder.SubItems, shares);
      }
    }

    /// <summary>
    /// Removes the share with the specified <paramref name="folder"/> from the local shares
    /// collection, if present.
    /// </summary>
    /// <param name="folder">Folder to remove. The containment check will be made via the
    /// <see cref="FolderItem.Folder"/> attribute.</param>
    protected void RemoveLocalShare(FolderItem folder)
    {
      for (int i = _shares.Count - 1; i >= 0; i--)
      {
        FolderItem share = (FolderItem) _shares[i];
        if (share.Folder == folder.Folder)
          _shares.Remove(share);
      }
      SynchronizeShares();
    }

    /// <summary>
    /// Adds the specified <paramref name="folder"/> share to the local shares
    /// collection, if not present yet.
    /// </summary>
    /// <param name="folder">Folder to add. The containment check will be made via the
    /// <see cref="FolderItem.Folder"/> attribute.</param>
    protected void AddLocalShare(FolderItem folder)
    {
      for (int i=_shares.Count-1; i >= 0; i--)
      {
        // Remove all shares which have the same path to root as the new folder
        FolderItem share = (FolderItem) _shares[i];
        if (share.Folder.StartsWith(folder.Folder) || folder.Folder.StartsWith(share.Folder))
          _shares.Remove(share);
      }
      _shares.Add(folder);
      SynchronizeShares();
    }

    protected void Refresh(ItemsList childrenList, FolderItem folder, bool addParent)
    {
      childrenList.Clear();
      if (folder == null)
      { // Refreshing the root folder
        string[] drives = Environment.GetLogicalDrives();
        for (int i = 0; i < drives.Length; ++i)
          childrenList.Add(new FolderItem(drives[i], drives[i], null));
      }
      else
      {
        // Refreshing a subfolder
        if (addParent)
          childrenList.Add(new FolderItem("..", folder.ParentFolder.Folder, folder.ParentFolder));
        try
        {
          foreach (string folderPath in Directory.GetDirectories(folder.Folder))
          {
            string folderName = Path.GetFileName(folderPath);
            childrenList.Add(new FolderItem(folderName, folderPath, folder));
          }
        }
        catch (IOException) { }
      }
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (FolderItem item in childrenList)
      {
        if (mgr.Shares.Contains(item.Folder))
          item.Selected = true;
      }
      SynchronizeShares();
      childrenList.FireChange();
    }
  }
}
