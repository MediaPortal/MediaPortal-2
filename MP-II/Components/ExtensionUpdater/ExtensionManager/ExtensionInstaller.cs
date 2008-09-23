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
using System.Xml.Serialization;
using Components.ExtensionUpdater.ExtensionManager.Actions;
using MediaPortal.Core;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Core.PathManager;
using Components.ExtensionUpdater.ExtensionManager;
using MediaPortal.Utilities.Screens;
using ICSharpCode.SharpZipLib.Zip;

namespace Components.ExtensionUpdater.ExtensionManager
{
  public class ExtensionInstaller: IExtensionInstaller
  {
    public delegate void OnNextFileEventHandler();


    #region variables

    private Dictionary<string, IExtensionFileAction> _fileActions;
    private ExtensionQueue Queue;
    public ExtensionEnumerator Enumerator;
    private ExtensionManagerSettings _settings;

    #endregion


    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionInstaller"/> class.
    /// </summary>
    public ExtensionInstaller()
    {
      Queue = new ExtensionQueue();
      Enumerator = new ExtensionEnumerator();
      _fileActions = new Dictionary<string, IExtensionFileAction>();
      _settings = new ExtensionManagerSettings();
      RegisterAction("CopyFile", new CopyFile());
      RegisterAction("ScreenShots", new ScreenShot());
      RegisterAction("CopyMenu", new CopyMenu());
      RegisterAction("Copy&RegisterFile", new CopyRegisterFile());
      RegisterAction("Copy&ExecuteFile", new CopyExecuteFile());
    }

    public List<ExtensionEnumeratorObject> GetAllKnowExtensions()
    {
      List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
      foreach (KeyValuePair<string, List<ExtensionEnumeratorObject>> kpv in Enumerator.Items)
      {
        ExtensionEnumeratorObject extensionItem = new ExtensionEnumeratorObject();
        foreach (ExtensionEnumeratorObject obj in kpv.Value)
        {
          if (extensionItem.Version.CompareTo(obj.Version) < 0)
            extensionItem = obj;
        }
        list.Add(extensionItem);
      }
      return list;
    }

    /// <summary>
    /// Test if a files is locked(used).
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns>True if found a locked file</returns>
    public bool AreFilesLocked(ExtensionPackage package)
    {
      foreach(ExtensionFileItem items in package.Items)
      {
        string filename = _fileActions[items.Action].GetDirEntry(items);
        if (File.Exists(filename))
        {
          try
          {
            FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Write);
            stream.Close();
          }
          catch (Exception)
          {
            return true;            
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Gets the unsolved dependencies.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public List<ExtensionEnumeratorObject> GetUnsolvedDependencies(ExtensionPackage package)
    {
      List<ExtensionEnumeratorObject> x_ret=new List<ExtensionEnumeratorObject>();
      foreach (ExtensionDependency dep in package.Dependencies)
      {
        ExtensionEnumeratorObject pak;
        if ((pak = Enumerator.GetInstalledExtesion(dep.ExtensionId)) == null)
        {
          x_ret.Add(Enumerator.GetExtensions(dep.ExtensionId));
        }
        //else
        //{
          
        //  switch (dep.Operator)
        //  {
        //    case ">=":
        //      if (pak.Version.CompareTo(dep.Version) < 0)
        //        if (!x_ret.Contains(pak))
        //          x_ret.Add(pak);
        //      break;
        //    case "<=":
        //      if (pak.Version.CompareTo(dep.Version) > 0)
        //        if (!x_ret.Contains(pak))
        //          x_ret.Add(pak);
        //      break;
        //    case "#":
        //      if (pak.Version == dep.Version)
        //        if (!x_ret.Contains(pak))
        //          x_ret.Add(pak);
        //      break;
        //    default:
        //      break;
        //  }
        //}
      }
      return x_ret;
    }
    /// <summary>
    /// Install a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public bool InstallPackage(IExtensionPackage package)
    {
      if (CheckDependency(package))
      {
        try
        {
          using (ZipInputStream s = new ZipInputStream(File.OpenRead(package.FileName)))
          {
            ZipEntry entry;
            while ((entry = s.GetNextEntry()) != null)
            {
              IExtensionFileItem fileitem = FindFileItemByZipEntry(entry.Name, package);
              if (fileitem != null)
              {
                _fileActions[fileitem.Action].Install(s, fileitem,package);
              }
              if (OnNextFile != null)
                OnNextFile();
            }
          }
        }
        catch (Exception ex)
        {
          System.Windows.Forms.MessageBox.Show(ex.Message);
          return false;
        }
        return true;
      }
      return false;
    }

    /// <summary>
    /// Uninstall a package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public bool UnInstallPackage(IExtensionPackage package)
    {
      foreach (IExtensionFileItem fileitem in ((ExtensionPackage)package).Items)
      {
        if (!_fileActions[fileitem.Action].UnInstall(null,fileitem , package))
          return false;
        if (OnNextFile != null)
          OnNextFile();
      }
      Enumerator.Add(package as ExtensionPackage, ExtensionPackageState.Local);
      Enumerator.Save();
      return true;
    }


    /// <summary>
    /// Checks the dependency.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public bool CheckDependency(IExtensionPackage package)
    {
      return true;
    }
    /// <summary>
    /// Loads the package from XML or XMP file.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <returns></returns>
    public IExtensionPackage LoadPackageFromXML(string filename)
    {
      try
      {
        ExtensionPackage pak = new ExtensionPackage();
        XmlSerializer serializer = new XmlSerializer(typeof(ExtensionPackage));
        FileStream fs = new FileStream(filename, FileMode.Open);
        pak = (ExtensionPackage)serializer.Deserialize(fs);
        fs.Close();
        //pak.FileName = filename;
        return pak;
      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Loads the package from MPI file .
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <returns> Return null if the file not exist</returns>
    public IExtensionPackage LoadPackageFromMPI(string filename)
    {
      if (!File.Exists(filename))
      {
        return null;
      }
      string tpf = Path.GetTempFileName();
      try
      {
        using (ZipInputStream s = new ZipInputStream(File.OpenRead(filename)))
        {
          byte[] data = new byte[2048];
          int nb = data.Length;
          ZipEntry entry;
          while ((entry = s.GetNextEntry()) != null)
          {
            if (entry.Name == "installer.xmp")
            {
              FileStream fs = new FileStream(tpf, FileMode.Create);
              while ((nb = s.Read(data, 0, data.Length)) > 0)
              {
                fs.Write(data, 0, nb);
              }
              fs.Close();
              break;
            }
          }
        }
        if (File.Exists(tpf))
        {
          IExtensionPackage pk =LoadPackageFromXML(tpf);
          pk.FileName = filename;
          File.Delete(tpf);
          return pk;
        }
      }
      catch (Exception)
      {
        return null;
      }
      return null;
    }

    #region FileActions

    public IExtensionFileAction GetFileAction(string action)
    {
      return _fileActions[action];
    }

    /// <summary>
    /// Return  all registered file actions.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, IExtensionFileAction> GetAllFileActions()
    {
      return _fileActions;
    }
    /// <summary>
    /// Registers the action.
    /// </summary>
    /// <param name="actionName">Name of the action.</param>
    /// <param name="action">The action class.</param>
    public void RegisterAction(string actionName, IExtensionFileAction action)
    {
      _fileActions[actionName] = action;
    }


    /// <summary>
    /// Finds the file item by zip entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns></returns>
    public IExtensionFileItem FindFileItemByZipEntry(string entry,IExtensionPackage package)
    {
      foreach (IExtensionFileItem item in ((ExtensionPackage)package).Items)
      {
        if (_fileActions[item.Action].GetZipEntry(item) == entry)
          return item;
      }
      return null;
    }

    #endregion

    #region Queue

    public void ExecuteQueue()
    {
      ExecuteQueue(true);
    }
    /// <summary>
    /// Executes the queue.
    /// </summary>
    public void ExecuteQueue(bool quiet)
    {
      if (Queue.Items.Count < 1)
        return;
      MPIScreen screen = new MPIScreen();
      if (!quiet)
      {
        screen.Show();
        OnNextFile += screen.NextFile;
      }
      screen.SetAllCount(Queue.Items.Count);
      ExtensionQueue temp_queue = new ExtensionQueue();
      foreach (ExtensionQueueObject item in Queue.Items)
      {
        if (!File.Exists(item.FileName))
        {
          ExtensionEnumeratorObject obj= Enumerator.GetItem(item.PackageId);
          if (obj != null && !string.IsNullOrEmpty(obj.DownloadUrl))
          {
            DownloadScreen dlScreen = new DownloadScreen(obj.DownloadUrl, item.FileName);
            dlScreen.ShowDialog();
          }
        }
        ExtensionPackage pak = (ExtensionPackage)LoadPackageFromMPI(item.FileName);
        screen.Next();
        if (pak != null)
        {
          screen.AddText(pak.Name + " - " + item.Action);
          screen.SetFileCount(pak.Items.Count);
          if (item.Action == "Install")
          {
            if (InstallPackage(pak))
            {
              temp_queue.Items.Add(item);
              ExtensionEnumeratorObject obj = Enumerator.GetItem(pak.PackageId);
              if (obj != null)
              {
                obj.State = ExtensionPackageState.Installed;
              }
              else
              {
                Enumerator.Add(pak, ExtensionPackageState.Installed);
              }
              Enumerator.Save();
            }
          }
          else
          {
            if (UnInstallPackage(pak))
            {
              ExtensionEnumeratorObject obj = Enumerator.GetItem(pak.PackageId);
              if (obj != null)
              {
                obj.State = ExtensionPackageState.Local;
                Enumerator.Save();
              }
              temp_queue.Items.Add(item);
            }
          }
        }
      }
      foreach (ExtensionQueueObject item in temp_queue.Items)
      {
        Queue.Items.Remove(item);
      }
      if (screen.Visible)
        screen.Close();
      SaveQueue();
    }
    
    /// <summary>
    /// Add a package to queue.
    /// </summary>
    /// <param name="package">The package.</param>
    public void AddToQueue(IExtensionPackage package, string action)
    {
      if (!Queue.Contains(new ExtensionQueueObject(package, action)))
      {
        try
        {
          string fullFileName = String.Format(@"{0}\{1}.mpi", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"), package.PackageId);
          if (!File.Exists(fullFileName) && File.Exists(package.FileName))
          {
            Directory.CreateDirectory(Path.GetDirectoryName(fullFileName));
            File.Copy(package.FileName, fullFileName, true);
          }
          package.FileName = fullFileName;
          Queue.Items.Add(new ExtensionQueueObject(package, action));
        }
        catch (Exception ex)
        {
          System.Windows.Forms.MessageBox.Show(ex.Message);
        }
      }
      SaveQueue();
    }

    /// <summary>
    /// Removes from queue.
    /// </summary>
    /// <param name="package">The package.</param>
    public void RemoveFromQueue(IExtensionPackage package)
    {
      RemoveFromQueue(package.PackageId);
    }
    /// <summary>
    /// Removes from queue.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    public void RemoveFromQueue(string packageId)
    {
      List<ExtensionQueueObject> removObj = new List<ExtensionQueueObject>();
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageId == packageId)
        {
          removObj.Add(Queue.Items[i]);
        }
      }
      foreach (ExtensionQueueObject obj in removObj)
      {
        Queue.Items.Remove(obj);
      }
      SaveQueue();
    }

    /// <summary>
    /// Removes all reference a extension from queue.
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    public void RemoveAllFromQueue(string extensionId)
    {
      List<ExtensionQueueObject> removObj = new List<ExtensionQueueObject>();
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageExtensionId == extensionId)
        {
          removObj.Add(Queue.Items[i]);
        }
      }
      foreach (ExtensionQueueObject obj in removObj)
      {
        Queue.Items.Remove(obj);
      }
      SaveQueue();
    }
    /// <summary>
    /// Gets the queue item.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <returns>If not found return null</returns>
    public ExtensionQueueObject GetQueueItem(string packageId)
    {
      ExtensionQueueObject item = null;
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageId == packageId)
        {
          item = Queue.Items[i];
        }
      }
      return item;
    }

    /// <summary>
    /// Gets the queue items.
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    /// <returns>List of items with same extension id</returns>
    public List<ExtensionQueueObject> GetQueueItems(string extensionId)
    {
      List<ExtensionQueueObject> items = new List<ExtensionQueueObject>();
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageExtensionId == extensionId)
        {
          items.Add(Queue.Items[i]);
        }
      }
      return items;
    }

    /// <summary>
    /// Return the queue.
    /// </summary>
    /// <returns></returns>
    public IExtensionQueue GetQueue()
    {
      return Queue;
    }

    public void LoadQueue()
    {
      try
      {
        string fullFileName = String.Format(@"{0}\Mpiqueue.xml", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
        if (File.Exists(fullFileName))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(ExtensionQueue));
          FileStream fs = new FileStream(fullFileName, FileMode.Open);
          Queue = (ExtensionQueue)serializer.Deserialize(fs);
          fs.Close();
        }
      }
      catch (Exception )
      {
      }
    }

    public void SaveQueue()
    {
      try
      {
        Directory.CreateDirectory(String.Format(@"{0}\MediaPortal\", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
        Directory.CreateDirectory(ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
        string fullFileName = String.Format(@"{0}\Mpiqueue.xml", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
        if (Queue.Items.Count == 0)
        {
          File.Delete(fullFileName);
        }
        else
        {
          XmlSerializer serializer = new XmlSerializer(typeof(ExtensionQueue));
          TextWriter writer = new StreamWriter(fullFileName);
          serializer.Serialize(writer, Queue);
          writer.Close();
        }
      }
      catch (Exception ex)
      {
        System.Windows.Forms.MessageBox.Show(ex.Message);
      }
    }
    #endregion

    /// <summary>
    /// Determines whether [is update waiting] [the specified extension id].
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    /// <returns>
    /// 	<c>true</c> if [is update waiting] [the specified extension id]; otherwise, <c>false</c>.
    /// </returns>
    public bool IsUpdateWaiting(string extensionId)
    {
      ExtensionEnumeratorObject obj = Enumerator.GetInstalledExtesion(extensionId);
      if (obj != null)
      {
        return Enumerator.HaveUpdate(obj);
      }
      return false;
    }

    /// <summary>
    /// Updates the specified extension 
    /// </summary>
    /// <param name="extensionId">The extension GUID</param>
    public void Update(string extensionId)
    {
      ExtensionEnumeratorObject latestObj = Enumerator.GetExtensions(extensionId);
      ExtensionEnumeratorObject installedObj = Enumerator.GetInstalledExtesion(extensionId);
      if (installedObj != null)
      {
        AddToQueue(installedObj, "Uninstall");
        foreach (ExtensionEnumeratorObject obj in GetUnsolvedDependencies(latestObj))
        {
          AddToQueue(obj, "Install");
        }
        AddToQueue(latestObj as IExtensionPackage, "Install");
      }
    }

    /// <summary>
    /// Updates all.
    /// </summary>
    public void UpdateAll()
    {

      foreach(ExtensionEnumeratorObject obj in GetAllKnowExtensions())
      {
        if(IsUpdateWaiting(obj.ExtensionId))
        {
          Update(obj.ExtensionId);
        }
      }
    }

    #region Events

    /// <summary>
    /// This event will trigger for every function that is not intercepted internally
    /// </summary>
    public event OnNextFileEventHandler OnNextFile;
    
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the settings.
    /// This setting isn't loaded automaticaly, this should loadede mamualy
    /// </summary>
    /// <value>The settings.</value>
    public ExtensionManagerSettings Settings
    {
      get
      {
        return _settings;
      }
      set
      {
        _settings = value;
      }
    }
    #endregion
  }
}