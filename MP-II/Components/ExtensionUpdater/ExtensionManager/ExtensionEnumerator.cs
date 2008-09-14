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
using System.Xml;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.ExtensionManager;
using MediaPortal.Core.PathManager;
using Components.ExtensionUpdater.ExtensionManager;

namespace Components.ExtensionUpdater.ExtensionManager
{
  public class ExtensionEnumerator
  {
    public ExtensionEnumerator()
    {
      _items = new Dictionary<string, List<ExtensionEnumeratorObject>>();
    }

    Dictionary<string, List<ExtensionEnumeratorObject>> _items;
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    /// <value>The items.</value>
    public Dictionary<string, List<ExtensionEnumeratorObject>> Items
    {
      get
      {
        if (_items.Count == 0)
          Load();
        return _items;
      }
      set
      {
        _items = value;
      }
    }

    /// <summary>
    /// Determines whether the specified extension id is installed.
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    /// <returns>
    /// 	<c>true</c> if the specified extension id is installed; otherwise, <c>false</c>.
    /// </returns>
    public bool IsInstalled(string extensionId)
    {
      if (Items.ContainsKey(extensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[extensionId])
        {
          if (obj.State == ExtensionPackageState.Installed)
          {
            return true;
          }
        }
      }
      else
      {
        return false;
      }
      return false;
    }

    /// <summary>
    /// Test if the extension have a newest version .
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    /// <returns></returns>
    public bool HaveUpdate(ExtensionEnumeratorObject pak)
    {
      if (Items.ContainsKey(pak.ExtensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[pak.ExtensionId])
        {
          if (obj.Compare(pak) >0)
          {
            return true;
          }
        }
      }
      else
      {
        return false;
      }
      return false;
    }

    public List<string> GetCategories()
    {
      List<string> x_ret = new List<string>();
      foreach (KeyValuePair<string, List<ExtensionEnumeratorObject>> kpv in Items)
      {
        foreach (ExtensionEnumeratorObject obj in kpv.Value)
        {
          if (!x_ret.Contains(obj.ExtensionType))
            x_ret.Add(obj.ExtensionType);
        }
      }
      return x_ret;
    }
    
    public ExtensionEnumeratorObject GetItem(string packageId)
    {
      foreach (KeyValuePair<string, List<ExtensionEnumeratorObject>> kpv in Items)
      {
        foreach (ExtensionEnumeratorObject obj in kpv.Value)
        {
          if (obj.PackageId == packageId)
            return obj;
        }
      }
      return null;
    }
    /// <summary>
    /// Gets the installed package.
    /// </summary>
    /// <param name="extensionId">The extension id.</param>
    /// <returns>Return null if the extension isn't installed</returns>
    public ExtensionEnumeratorObject GetInstalledExtesion(string extensionId)
    {
      if (Items.ContainsKey(extensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[extensionId])
        {
          if (obj.State == ExtensionPackageState.Installed)
          {
            return obj;
          }
        }
      }
      else
      {
        return null;
      }
      return null;
    }

    /// <summary>
    /// Return the hightes version number extesion.
    /// </summary>
    /// <param name="extesionId">The extesion id.</param>
    /// <returns></returns>
    public ExtensionEnumeratorObject GetExtensions(string extensionId)
    {
      ExtensionEnumeratorObject extensionItem = new ExtensionEnumeratorObject();
      if (Items.ContainsKey(extensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[extensionId])
        {
          if (extensionItem.Version.CompareTo(obj.Version) < 0)
            extensionItem = obj;
        }
      }
      return extensionItem;
    }

    /// <summary>
    /// Adds the specified package.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="state">The state.</param>
    public void Add(ExtensionPackage package,ExtensionPackageState state)
    {
      if (Items.ContainsKey(package.ExtensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[package.ExtensionId])
        {
          if ((obj.State == state)&& state == ExtensionPackageState.Installed)
          {
            if (File.Exists(obj.FileName))
              obj.State = ExtensionPackageState.Local;
            else
              obj.State = ExtensionPackageState.Unknown;
          }
        }
        foreach (ExtensionEnumeratorObject obj in Items[package.ExtensionId])
        {
          if (obj.PackageId == package.PackageId)
          {
            Items[package.ExtensionId].Remove(obj);
            break;
          }
        }
        Items[package.ExtensionId].Add(new ExtensionEnumeratorObject(package,state));
      }
      else
      {
        List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
        list.Add(new ExtensionEnumeratorObject(package, state));
        Items.Add(package.ExtensionId, list);
      }
    }

    /// <summary>
    /// Adds the specified package.
    /// </summary>
    /// <param name="package">The package.</param>
    public void Add(ExtensionPackage package)
    {
      bool found = false;
      if (Items.ContainsKey(package.ExtensionId))
      {
        foreach (ExtensionEnumeratorObject obj in Items[package.ExtensionId])
        {
          if (obj.PackageId == package.PackageId)
          {
            obj.FileName = package.FileName;
            obj.Dependencies = package.Dependencies;
            obj.Items = package.Items;
            found = true;
            break;
          }
        }
        if (!found)
          Items[package.ExtensionId].Add(new ExtensionEnumeratorObject(package,ExtensionPackageState.Unknown));
      }
      else
      {
        List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
        list.Add(new ExtensionEnumeratorObject(package, ExtensionPackageState.Unknown));
        Items.Add(package.ExtensionId, list);
      }
    }

    /// <summary>
    /// Adds the specified package.
    /// </summary>
    /// <param name="package">The package.</param>
    public void Add(ExtensionEnumeratorObject package)
    {
      if (Items.ContainsKey(package.ExtensionId))
      {
        bool found = false;
        foreach (ExtensionEnumeratorObject obj in Items[package.ExtensionId])
        {
          if (obj.PackageId == package.PackageId)
          {
            obj.DownloadUrl = package.DownloadUrl;
            obj.Downloads = package.Downloads;
            obj.Description = package.Description;
            found = true;
            break;
          }
        }
        if (!found)
          Items[package.ExtensionId].Add(package);
      }
      else
      {
        List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
        list.Add(package);
        Items.Add(package.ExtensionId, list);
      }
    }

    #region Load/Save
    /// <summary>
    /// Updates the list from OpenMaid delivered file.
    /// </summary>
    /// <param name="listFile">The list file.</param>
    public void UpdateList(string listFile)
    {
      List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
      if (File.Exists(listFile))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(listFile);
        XmlNodeList extList = doc.SelectNodes("plugins/plugin");
        foreach (XmlNode nodeext in extList)
        {
          ExtensionEnumeratorObject obj = new ExtensionEnumeratorObject();
          obj.State = ExtensionPackageState.Online;
          obj.ExtensionId = nodeext.Attributes["id"].Value;
          obj.PackageId = nodeext.Attributes["packageid"].Value;
          obj.Version = nodeext.Attributes["version"].Value;
          obj.VersionType = nodeext.Attributes["state"].Value;
          obj.ExtensionType = nodeext.Attributes["type"].Value;
          obj.Name = nodeext.Attributes["name"].Value;
          obj.DownloadUrl = nodeext.Attributes["url"].Value;
          obj.Author = nodeext.Attributes["author"].Value;
          obj.Description = nodeext.Attributes["desc"].Value;

          DateTime d = obj.Date;
          DateTime.TryParse(nodeext.Attributes["date"].Value, out d);
          obj.Date = d;

          int i = 0;
          int.TryParse(nodeext.Attributes["desc"].Value, out i);
          obj.Downloads = i;

          i = 0;
          int.TryParse(nodeext.Attributes["size"].Value, out i);
          obj.Size = i;

          Add(obj);
        }
      }
    }
    /// <summary>
    /// Loads this instance from file.
    /// </summary>
    public void Load()
    {
      string fullFileName = String.Format(@"{0}\MPIRegistry.xml",ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
      if (File.Exists(fullFileName))
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(fullFileName);
        this._items.Clear();
        XmlNode ver = doc.DocumentElement.SelectSingleNode("/MPIRegistry");
        XmlNodeList extList = ver.SelectNodes("Extension");
        foreach (XmlNode nodeext in extList)
        {
          List<ExtensionEnumeratorObject> list = new List<ExtensionEnumeratorObject>();
          string ext = nodeext.Attributes["Id"].Value;
          foreach (XmlNode nodepak in nodeext.SelectNodes("Package"))
          {
            ExtensionEnumeratorObject obj = new ExtensionEnumeratorObject();
            list.Add(obj);
            obj.ExtensionId = ext;
            obj.PackageId = nodepak.Attributes["Id"].Value;
            obj.Name = nodepak.Attributes["Name"].Value;
            obj.Version = nodepak.Attributes["Version"].Value;
            obj.VersionType = nodepak.Attributes["VersionType"].Value;
            obj.ExtensionType = nodepak.Attributes["ExtensionType"].Value;
            obj.FileName = nodepak.Attributes["FileName"].Value;
            obj.Author = nodepak.Attributes["Author"].Value;
            obj.Description = nodepak.Attributes["Description"].Value;
            obj.DownloadUrl = nodepak.Attributes["DownloadUrl"].Value;
            
            DateTime d = obj.Date;
            DateTime.TryParse(nodepak.Attributes["Date"].Value,out d);
            obj.Date = d;

            int i = 0;
            int.TryParse(nodepak.Attributes["Downloads"].Value,out i);
            obj.Downloads = i;
            
            i = 0;
            int.TryParse(nodepak.Attributes["Size"].Value, out i);
            obj.Size = i;

            switch (nodepak.Attributes["State"].Value)
            {
              case "Local":
                obj.State = ExtensionPackageState.Local;
                break;
              case "Installed":
                obj.State = ExtensionPackageState.Installed;
                break;
              case "Online":
                obj.State = ExtensionPackageState.Online;
                break;
              case "System":
                obj.State = ExtensionPackageState.System;
                break;
              default :
                obj.State = ExtensionPackageState.Unknown;
                break;
            }
            if (string.IsNullOrEmpty(obj.FileName))
            {
              obj.FileName = String.Format(@"{0}\{1}.mpi", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"), obj.PackageId);
            }
            if (obj.State == ExtensionPackageState.Local && !File.Exists(obj.FileName))
            {
              obj.State = ExtensionPackageState.Online;
            }
            foreach (XmlLinkedNode depnode in nodepak.SelectNodes("Dependencies/Dependency"))
            {
              if (depnode.Attributes.Count > 0)
                obj.Dependencies.Add(new ExtensionDependency(depnode.Attributes["Id"].Value, depnode.Attributes["Operator"].Value, depnode.Attributes["Version"].Value));
            }
            foreach (XmlLinkedNode filnode in nodepak.SelectNodes("Files/File"))
            {
              obj.Items.Add(new ExtensionFileItem(filnode.Attributes["FileName"].Value,
                                                  filnode.Attributes["Action"].Value, filnode.Attributes["Param1"].Value, filnode.Attributes["Param2"].Value, filnode.Attributes["Param3"].Value));
            }
          }
          _items.Add(ext, list);
        }
      }
    }

    /// <summary>
    /// Saves this instance to file.
    /// </summary>
    public void Save()
    {
      Directory.CreateDirectory(ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
      string fullFileName = String.Format(@"{0}\MPIRegistry.xml", ServiceScope.Get<IPathManager>().GetPath("<MPINSTALLER>"));
      Stream myStream;
      if ((myStream = File.Open(fullFileName, FileMode.Create, FileAccess.Write, FileShare.None)) != null)
      {
        // Code to write the stream goes here.
        XmlDocument doc = new XmlDocument();
        XmlWriter writer = null;
        try
        {
          // Create an XmlWriterSettings object with the correct options. 
          XmlWriterSettings settings = new XmlWriterSettings();
          string st = string.Empty;
          settings.Indent = true;
          settings.IndentChars = ("\t");
          settings.OmitXmlDeclaration = true;
          // Create the XmlWriter object and write some content.
          writer = XmlWriter.Create(myStream, settings);
          writer.WriteStartElement("MPIRegistry");
          foreach (KeyValuePair<string, List<ExtensionEnumeratorObject>> key in Items)
          {
            writer.WriteStartElement("Extension");
            writer.WriteAttributeString("Id", key.Key);
            foreach (ExtensionEnumeratorObject ob in key.Value)
            {
              writer.WriteStartElement("Package");
              writer.WriteAttributeString("Id", ob.PackageId);
              writer.WriteAttributeString("Name", ob.Name);
              writer.WriteAttributeString("Version", ob.Version);
              writer.WriteAttributeString("VersionType", ob.VersionType);
              writer.WriteAttributeString("ExtensionType", ob.ExtensionType);
              writer.WriteAttributeString("State", ob.State.ToString());
              writer.WriteAttributeString("FileName", ob.FileName);
              writer.WriteAttributeString("Author", ob.Author);
              writer.WriteAttributeString("Description", ob.Description);
              writer.WriteAttributeString("DownloadUrl", ob.DownloadUrl);
              writer.WriteAttributeString("Downloads", ob.Downloads.ToString());
              writer.WriteAttributeString("Date", ob.Date.ToString());
              writer.WriteAttributeString("Size", ob.Size.ToString());
              writer.WriteStartElement("Dependencies");
              foreach (ExtensionDependency dep in ob.Dependencies)
              {
                writer.WriteStartElement("Dependency");
                writer.WriteAttributeString("Id", dep.ExtensionId);
                writer.WriteAttributeString("Operator", dep.Operator);
                writer.WriteAttributeString("Version", dep.Version);
                writer.WriteEndElement();
              }
              writer.WriteEndElement();
              writer.WriteStartElement("Files");
              foreach (IExtensionFileItem fil in ob.Items)
              {
                writer.WriteStartElement("File");
                writer.WriteAttributeString("FileName", Path.GetFileName(fil.FileName));
                writer.WriteAttributeString("Action", fil.Action);
                writer.WriteAttributeString("Param1", fil.Param1);
                writer.WriteAttributeString("Param2", fil.Param2);
                writer.WriteAttributeString("Param3", fil.Param3);
                writer.WriteEndElement();
              }
              writer.WriteEndElement();
              writer.WriteEndElement();
            }
            writer.WriteEndElement();
          }
          writer.WriteEndElement();
        }
        finally
        {
          if (writer != null)
            writer.Close();
        }
        myStream.Close();
      }
    }
    #endregion

  }
}