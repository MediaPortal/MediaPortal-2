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
using System.IO;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Presentation.WindowManager;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;

using MediaPortal.Media.Importers;

namespace Shares
{
  public class Model : IPlugin
  {
    ItemsCollection _folders = new ItemsCollection();
    ItemsCollection _shares = new ItemsCollection();
    ListItem _selectedItem;

    #region IPlugin Members
    public void Initialize(string id)
    {
    }

    public void Dispose()
    {
    }

    public Model()
    {
      Refresh(_folders, null, true);
      RefreshShares();
    }
    #endregion

    public ItemsCollection Shares
    {
      get
      {
        return _shares;
      }
    }

    public void RemoveShare(ListItem item)
    {
      if (item == null) return;
      string path = item.Labels["Path"].Evaluate(null, null);
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      mgr.RemoveShare(path);
      RefreshShares();
    }
    public ItemsCollection NoSharesMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("shares-notdefined-main"));
      }
    }
    public ItemsCollection SharesRemoveMainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("shares-remove-main"));
      }
    }
    public ItemsCollection SharesAddMainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("shares-add-main"));
      }
    }


    public void RefreshShares()
    {
      _shares.Clear();
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (string share in mgr.Shares)
      {
        ListItem item = new ListItem();
        item.Add("Name", share);
        item.Add("Path", share);
        _shares.Add(item);
      }
      _shares.FireChange();
    }

    public ItemsCollection MainMenu
    {
      get
      {
        IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
        return new ItemsCollection(menuCollect.GetMenu("shares-main"));
      }
    }


    public ItemsCollection Folders
    {
      get
      {
        return _folders;
      }
    }

    public ListItem SelectedItem
    {
      get
      {
        return _selectedItem;
      }
      set
      {
        _selectedItem = value;
      }
    }


    public void SelectSubItems(ItemsCollection items, bool enabled)
    {
      foreach (ListItem item in items)
      {
        FolderItem folder = (FolderItem)item;
        if (enabled)
        {
          item.Add("selected", "true");
        }
        else
        {
          item.Add("selected", "false");
        }
        if (item.SubItems != null && item.SubItems.Count > 0)
        {
          SelectSubItems(item.SubItems, enabled);
        }
        item.FireChange();
      }
    }
    public void SelectListItem(ListItem item)
    {
      if (item == null) return;
      bool enabled = false;
      if (item.Labels["selected"].Evaluate(null, null) == "false")
      {
        item.Add("selected", "true");
        enabled = true;
      }
      else
      {
        item.Add("selected", "false");
      }

      if (item.SubItems != null && item.SubItems.Count > 0)
      {
        SelectSubItems(item.SubItems, enabled);
      }

      item.FireChange();
    }

    public void DisableSubItems(ItemsCollection collection)
    {
      if (collection.Count == 0) return;
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (FolderItem item in collection)
      {
        mgr.RemoveShare(item.Folder);
        if (item.SubItems != null && item.SubItems.Count > 0)
          DisableSubItems(item.SubItems);
      }
    }

    public void Save()
    {
      SaveItems(_folders);
      RefreshShares();
      ServiceScope.Get<IWindowManager>().ShowPreviousWindow();
    }
    void SaveItems(ItemsCollection items)
    {
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (FolderItem item in items)
      {
        if (item.Selected)
        {
          mgr.AddShare(item.Folder);
          if (item.SubItems != null && item.SubItems.Count > 0)
            DisableSubItems(item.SubItems);
        }
        else
        {
          mgr.RemoveShare(item.Folder);
          if (item.SubItems != null && item.SubItems.Count > 0)
            SaveItems(item.SubItems);
        }
      }
    }

    public void GetSubItems(ListItem listItem)
    {
      if (listItem == null) return;
      FolderItem folderItem = listItem as FolderItem;
      if (folderItem == null) return;
      Refresh(listItem.SubItems, folderItem, false);
    }

    public void Refresh(ItemsCollection folders, FolderItem folder, bool addParent)
    {
      if (folder != null)
      {
        if (folder.Labels["selected"].Evaluate(null, null) == "true")
          return;
      }
      folders.Clear();
      if (folder == null)
      {
        string[] drives = Environment.GetLogicalDrives();
        for (int i = 0; i < drives.Length; ++i)
        {
          AddItem(folders, new FolderItem(drives[i], drives[i], null));
        }
      }
      else
      {
        if (addParent)
          AddItem(folders, new FolderItem("..", "..", folder.ParentFolder));

        try
        {
          string[] folderList = System.IO.Directory.GetDirectories(folder.Folder);
          for (int i = 0; i < folderList.Length; ++i)
          {
            string folderName;
            int pos = folderList[i].LastIndexOf(@"\");
            if (pos > 0)
              folderName = folderList[i].Substring(pos + 1);
            else
              folderName = folderList[i];

            AddItem(folders, new FolderItem(folderName, folderList[i], folder));
          }
        }
        catch (Exception)
        {
        }
      }
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (string share in mgr.Shares)
      {
        foreach (FolderItem item in folders)
        {
          if (share == item.Folder)
          {
            item.Selected = true;
            item.Add("selected", "true");
          }
        }
      }
      folders.FireChange(true);
    }
    void AddItem(ItemsCollection folders, FolderItem newItem)
    {
      //      if (folders.Count >= 5) return;
      folders.Add(newItem);
    }

    /// <summary>
    /// Starts a database refresh for all defined shares
    /// </summary>
    public void RefreshLibrary()
    {
      IImporterManager mgr = ServiceScope.Get<IImporterManager>();
      foreach (string share in mgr.Shares)
      {
        mgr.ForceImport(share,true);
      }
    }
  }
}
