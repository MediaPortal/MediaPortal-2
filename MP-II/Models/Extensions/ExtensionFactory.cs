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
using System.Net;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Services.ExtensionManager;
using MediaPortal.Media.MediaManager;

namespace Models.Extensions
{
  public class ExtensionFactory
  {
    #region events


    #endregion

    public ExtensionInstaller Installer = ServiceScope.Get<IExtensionInstaller>() as ExtensionInstaller;
    public int _view;

    #region shares
    public FolderItem GetFolder(string folder)
    {
      return null;
    }

    public void DownloadExtraInfo(ExtensionEnumeratorObject obj)
    {
      string source = string.Format("{3}/plugins/{0}/{1}/{2}/plugin.xml",obj.ExtensionId,obj.Version,obj.VersionType,Installer.Settings.BaseUrl);
      string dest = Path.GetTempFileName();
      DownloadFile(source, dest);
      IExtensionPackage pak = Installer.LoadPackageFromXML(dest);
      if (pak != null)
        Installer.Enumerator.Add((ExtensionPackage)pak);
      Installer.Enumerator.Save();
    }

    public bool DownloadFile(string url, string file)
    {
      bool x_ret = true;
      using (WebClient client = new WebClient())
      {
        try
        {
          //window.WaitCursorVisible = true;
          client.Credentials = new NetworkCredential("test", "testmaid5");
          client.DownloadFile(url, file);
        }
        catch (Exception ex)
        {
          ServiceScope.Get<ILogger>().Error("MPIManager: Failed to download file: {0} {1} {2}", ex.Message,
                                           ex.Source, ex.StackTrace);
          x_ret = false;
        }
        finally
        {
          //window.WaitCursorVisible = false;
        }
      }
      return x_ret;
    }
    
    private void LoadCategories(ref ItemsCollection extensions, FolderItem item)
    {
      foreach (string cat in Installer.Enumerator.GetCategories())
      {
        FolderItem parentItem = new FolderItem();
        parentItem.Add("Name", cat);
        parentItem.Add("Size", "");
        parentItem.Add("Date", "");
        parentItem.Add("State", "");
        parentItem.Add("CoverArt", "DefaultFolderBig.png");
        extensions.Add(parentItem);
      }
    }

    public void LoadItems(ref ItemsCollection extensions, List<ExtensionEnumeratorObject> extensionList)
    {
      foreach (ExtensionEnumeratorObject extensionItem in extensionList)
      {
        ExtensionItem newItem = new ExtensionItem(extensionItem);
        newItem.Add("Name", extensionItem.Name + " - v" + extensionItem.Version);
        newItem.Add("Size", "");
        newItem.Add("Date", "");
        newItem.Add("State", GetState(extensionItem));
        newItem.Add("CoverArt", "DefaultExtension.png");
        extensions.Add(newItem);
      }
    }

    private void LoadItems(ref ItemsCollection extensions, FolderItem item, string cat)
    {
      foreach (KeyValuePair<string, List<ExtensionEnumeratorObject>> kpv in Installer.Enumerator.Items)
      {
        ExtensionEnumeratorObject extensionItem = Installer.Enumerator.GetExtensions(kpv.Key);
        ExtensionPackage pak;
        if ((pak = Installer.Enumerator.GetInstalledExtesion(extensionItem.ExtensionId)) != null)
        {
          extensionItem = new ExtensionEnumeratorObject(pak, ExtensionPackageState.Installed);
        }
        if (!string.IsNullOrEmpty(extensionItem.Name) && MetView(extensionItem))
        {
          ExtensionItem newItem = new ExtensionItem(extensionItem);
          newItem.Add("Name", extensionItem.Name);
          newItem.Add("Size", "");
          newItem.Add("Date", "");
          newItem.Add("State", GetState(extensionItem));
          newItem.Add("CoverArt", GetThumb(extensionItem));
          extensions.Add(newItem);
        }
      }
    }

    public void LoadExtensions(ref ItemsCollection extensions, FolderItem item)
    {
      Installer.Enumerator.Load();
      extensions.Clear();
      if (item == null || item.Labels["Name"].Evaluate()=="..")
      {
        if (_view != 2)
          LoadCategories(ref extensions, item);
        else
          LoadItems(ref extensions, item,string.Empty);
      }
      else 
      {
        if (_view != 2)
        {
          FolderItem parentItem;
          parentItem = new FolderItem(item as IRootContainer);
          parentItem.Add("Name", "..");
          parentItem.Add("Size", "");
          parentItem.Add("Date", "");
          parentItem.Add("State", "");
          parentItem.Add("CoverArt", "DefaultFolderBackBig.png");
          extensions.Add(parentItem);
        }
        string cat = item.Labels["Name"].Evaluate();
        if (_view == 2)
          cat = string.Empty;
        LoadItems(ref extensions, item, cat);
      }
    }

    public string GetThumb(ExtensionEnumeratorObject obj)
    {
      if (obj.ContainScreenShoot())
      {
        Random rand=new Random();
        return string.Format("http://sandbox.team-mediaportal.com/plugins/{0}/{1}/{2}/{3}.jpg", obj.ExtensionId, obj.Version, obj.VersionType, rand.Next(obj.NumScreenShoots() - 1)+1);
      }
      else
      {
        return "DefaultExtension.png";
      }
    }

    private string GetState(ExtensionEnumeratorObject ext)
    {
      string  x_ret = "Unknown";
      if (ext.State == ExtensionPackageState.Installed)
      {
        x_ret = "Installed";
        if (Installer.Enumerator.HaveUpdate(ext))
          x_ret = "Update";
      }
      if (ext.State == ExtensionPackageState.Online)
        x_ret = "Online";
      ExtensionQueueObject queueItem = Installer.GetQueueItem(ext.PackageId);
      if (queueItem != null)
      {
        if (ext.State == ExtensionPackageState.Installed && queueItem.Action == "Install")
        {
          x_ret = "ReInstall";
        }
        else
        {
          if (queueItem.Action == "Install")
            x_ret = "QueueInstall";
          else
            x_ret = "QueueUnInstall";
        }
      }
      return x_ret;
    }

    private bool MetView(ExtensionEnumeratorObject ext)
    {
      switch (_view)
      {
        case 1:
          if (ext.State == ExtensionPackageState.Installed || Installer.GetQueueItem(ext.PackageId) != null)
            return true;
          else
            return false;
        default:
          break;
      }
      return true;
    }

    #endregion

    #region helper methods

    private static void AddItem(ref ItemsCollection songs, IMediaItem mediaItem)
    {
      Uri uri = mediaItem.ContentUri;
      if (uri != null)
      {
        string filename = uri.AbsolutePath.ToLower();
        if (!IsSong(filename))
        {
          return;
        }
      }

      Dictionary<string, object> metadata = mediaItem.MetaData;
      ExtensionItem newItem = new ExtensionItem(mediaItem);
      newItem.Add("Name", mediaItem.Title);
      newItem.Add("CoverArt", "defaultAudioBig.png");
      Dictionary<string, object>.Enumerator enumer = metadata.GetEnumerator();
      while (enumer.MoveNext())
      {
        if (enumer.Current.Value != null)
        {
          if (enumer.Current.Key == "Size")
          {
            newItem.Add(enumer.Current.Key, FileUtil.GetSize((long)enumer.Current.Value));
          }
          else if (enumer.Current.Value.GetType() == typeof(DateTime))
          {
            newItem.Add(enumer.Current.Key, ((DateTime)(enumer.Current.Value)).ToString("yyyy-MM-dd hh:mm:ss"));
          }
          else
          {
            newItem.Add(enumer.Current.Key, enumer.Current.Value.ToString());
          }
        }
      }

      if (!newItem.MediaItem.MetaData.ContainsKey("CoverArt"))
        newItem.MediaItem.MetaData["CoverArt"] = "defaultAudioBig.png";
      newItem.MediaItem.MetaData["defaulticon"] = "defaultAudioBig.png";
      newItem.Add("defaulticon", "defaultAudioBig.png");

      songs.Add(newItem);
    }

    private static bool IsSong(string fileName)
    {
      if (fileName.IndexOf(".mp3") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".wav") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".wma") >= 0)
      {
        return true;
      }
      if (fileName.IndexOf(".flac") >= 0)
      {
        return true;
      }
      return false;
    }

    #endregion

  }
}
