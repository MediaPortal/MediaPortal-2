#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Text;
using System.IO;
using System.Xml.Serialization;
using MediaPortal.Core.MPIManager;
using MediaPortal.Services.MPIManager.Actions;
using MediaPortal.Utilities.Screens;
using ICSharpCode.SharpZipLib.Zip;

namespace MediaPortal.Services.MPIManager
{
  public class MPInstaller: IMPInstaller
  {
    public delegate void OnNextFileEventHandler();


    #region variables

    private Dictionary<string, IMPIFileAction> _fileActions;
    private MPIQueue Queue;
    public MPIEnumerator Enumerator;

    #endregion


    /// <summary>
    /// Initializes a new instance of the <see cref="MPInstaller"/> class.
    /// </summary>
    public MPInstaller()
    {
      Queue = new MPIQueue();
      Enumerator = new MPIEnumerator();
      _fileActions = new Dictionary<string, IMPIFileAction>();
      RegisterAction("CopyFile", new CopyFile());
      RegisterAction("ScreenShots", new ScreenShot());
      RegisterAction("CopyMenu", new CopyMenu());
      RegisterAction("Copy&RegisterFile", new CopyRegisterFile());
      RegisterAction("Copy&ExecuteFile", new CopyExecuteFile());
    }

    public List<MPIEnumeratorObject> GetAllKnowExtensions()
    {
      List<MPIEnumeratorObject> list = new List<MPIEnumeratorObject>();
      foreach (KeyValuePair<string, List<MPIEnumeratorObject>> kpv in Enumerator.Items)
      {
        MPIEnumeratorObject extensionItem = new MPIEnumeratorObject();
        foreach (MPIEnumeratorObject obj in kpv.Value)
        {
          if (extensionItem.Version.CompareTo(obj.Version) < 0)
            extensionItem = obj;
        }
        list.Add(extensionItem);
      }
      return list;
    }
    
    /// <summary>
    /// Gets the unsolved dependencies.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public List<MPIEnumeratorObject> GetUnsolvedDependencies(MPIPackage package)
    {
      List<MPIEnumeratorObject> x_ret=new List<MPIEnumeratorObject>();
      foreach (MPIDependency dep in package.Dependencies)
      {
        MPIEnumeratorObject pak;
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
    public bool InstallPackage(IMPIPackage package)
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
              IMPIFileItem fileitem = FindFileItemByZipEntry(entry.Name, package);
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
    public bool UnInstallPackage(IMPIPackage package)
    {
      foreach (IMPIFileItem fileitem in ((MPIPackage)package).Items)
      {
        if (!_fileActions[fileitem.Action].UnInstall(null,fileitem , package))
          return false;
        if (OnNextFile != null)
          OnNextFile();
      }
      Enumerator.Add(package as MPIPackage, MPIPackageState.Local);
      Enumerator.Save();
      return true;
    }


    /// <summary>
    /// Checks the dependency.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns></returns>
    public bool CheckDependency(IMPIPackage package)
    {
      return true;
    }
    /// <summary>
    /// Loads the package from XML or XMP file.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <returns></returns>
    public IMPIPackage LoadPackageFromXML(string filename)
    {
      try
      {
        MPIPackage pak = new MPIPackage();
        XmlSerializer serializer = new XmlSerializer(typeof(MPIPackage));
        FileStream fs = new FileStream(filename, FileMode.Open);
        pak = (MPIPackage)serializer.Deserialize(fs);
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
    public IMPIPackage LoadPackageFromMPI(string filename)
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
          IMPIPackage pk =LoadPackageFromXML(tpf);
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

    public IMPIFileAction GetFileAction(string action)
    {
      return _fileActions[action];
    }

    /// <summary>
    /// Return  all registered file actions.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, IMPIFileAction> GetAllFileActions()
    {
      return _fileActions;
    }
    /// <summary>
    /// Registers the action.
    /// </summary>
    /// <param name="actionName">Name of the action.</param>
    /// <param name="action">The action class.</param>
    public void RegisterAction(string actionName, IMPIFileAction action)
    {
      _fileActions[actionName] = action;
    }


    /// <summary>
    /// Finds the file item by zip entry.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <returns></returns>
    public IMPIFileItem FindFileItemByZipEntry(string entry,IMPIPackage package)
    {
      foreach (IMPIFileItem item in ((MPIPackage)package).Items)
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
      MPIQueue temp_queue = new MPIQueue();
      foreach (MPIQueueObject item in Queue.Items)
      {
        if (!File.Exists(item.FileName))
        {
          MPIEnumeratorObject obj= Enumerator.GetItem(item.PackageId);
          if (obj != null && !string.IsNullOrEmpty(obj.DownloadUrl))
          {
            DownloadScreen dlScreen = new DownloadScreen(obj.DownloadUrl, item.FileName);
            dlScreen.ShowDialog();
          }
        }
        MPIPackage pak = (MPIPackage)LoadPackageFromMPI(item.FileName);
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
              MPIEnumeratorObject obj = Enumerator.GetItem(pak.PackageId);
              if (obj != null)
              {
                obj.State = MPIPackageState.Installed;
              }
              else
              {
                Enumerator.Add(pak, MPIPackageState.Installed);
              }
              Enumerator.Save();
            }
          }
          else
          {
            if (UnInstallPackage(pak))
            {
              MPIEnumeratorObject obj = Enumerator.GetItem(pak.PackageId);
              if (obj != null)
              {
                obj.State = MPIPackageState.Local;
                Enumerator.Save();
              }
              temp_queue.Items.Add(item);
            }
          }
        }
      }
      foreach (MPIQueueObject item in temp_queue.Items)
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
    public void AddToQueue(IMPIPackage package, string action)
    {
      if (!Queue.Contains(new MPIQueueObject(package, action)))
      {
        try
        {
          string fullFileName = String.Format(@"{0}\MediaPortal\Installer\{1}.mpi", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), package.PackageId);
          if (!File.Exists(fullFileName) && File.Exists(package.FileName))
          {
            Directory.CreateDirectory(Path.GetDirectoryName(fullFileName));
            File.Copy(package.FileName, fullFileName, true);
          }
          package.FileName = fullFileName;
          Queue.Items.Add(new MPIQueueObject(package, action));
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
    public void RemoveFromQueue(IMPIPackage package)
    {
      RemoveFromQueue(package.PackageId);
    }
    /// <summary>
    /// Removes from queue.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    public void RemoveFromQueue(string packageId)
    {
      List<MPIQueueObject> removObj = new List<MPIQueueObject>();
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageId == packageId)
        {
          removObj.Add(Queue.Items[i]);
        }
      }
      foreach (MPIQueueObject obj in removObj)
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
      List<MPIQueueObject> removObj = new List<MPIQueueObject>();
      for (int i = 0; i < Queue.Items.Count; i++)
      {
        if (Queue.Items[i].PackageExtensionId == extensionId)
        {
          removObj.Add(Queue.Items[i]);
        }
      }
      foreach (MPIQueueObject obj in removObj)
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
    public MPIQueueObject GetQueueItem(string packageId)
    {
      MPIQueueObject item = null;
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
    public List<MPIQueueObject> GetQueueItems(string extensionId)
    {
      List<MPIQueueObject> items = new List<MPIQueueObject>();
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
    public IMPIQueue GetQueue()
    {
      return Queue;
    }

    public void LoadQueue()
    {
      try
      {
        string fullFileName = String.Format(@"{0}\MediaPortal\Installer\Mpiqueue.xml", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        if (File.Exists(fullFileName))
        {
          XmlSerializer serializer = new XmlSerializer(typeof(MPIQueue));
          FileStream fs = new FileStream(fullFileName, FileMode.Open);
          Queue = (MPIQueue)serializer.Deserialize(fs);
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
        Directory.CreateDirectory(
          String.Format(@"{0}\MediaPortal\Installer\",
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
        string fullFileName = String.Format(@"{0}\MediaPortal\Installer\Mpiqueue.xml", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        if (Queue.Items.Count == 0)
        {
          File.Delete(fullFileName);
        }
        else
        {
          XmlSerializer serializer = new XmlSerializer(typeof(MPIQueue));
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

    #region Events

    /// <summary>
    /// This event will trigger for every function that is not intercepted internally
    /// </summary>
    public event OnNextFileEventHandler OnNextFile;
    
    #endregion
  }
}
