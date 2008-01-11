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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Importers;
using MediaPortal.Core.MediaManager;

namespace MediaPortal.Plugins.Services.Importers
{
  class ImporterBuilder : IPluginBuilder
  {
    #region variables
    string _type;
    string _extensions;
    INodeItem _item;
    IImporter _importerInstance;
    #endregion

    #region IPluginBuilder methods
    public object BuildItem(object caller, INodeItem item, ArrayList subItems)
    {
      ImporterBuilder builder = new ImporterBuilder();
      builder._item = item;

      if (item.Contains("extensions"))
      {
        builder._extensions = item["extensions"];
      }

      if (item.Contains("type"))
      {
        builder._type = item["type"];
      }

      return builder;
    }
    #endregion

    #region IImporterBuilder
    /// <summary>
    /// Gets the importer name.
    /// </summary>
    /// <value>The importer name.</value>
    public string Name
    {
      get { return _item.Id; }
    }

    /// <summary>
    /// Gets the file-extensions the importer supports
    /// </summary>
    /// <value>The file-extensions.</value>
    public string Extensions
    {
      get { return _extensions; }
    }

    /// <summary>
    /// Gets the Importer
    /// </summary>
    /// <value>The Importer instance.</value>
    public IImporter Importer
    {
      get
      {
        Build();
        return _importerInstance;
      }
    }

    /// <summary>
    /// Called by the importer manager when a full-import needs to be done from the folder
    /// </summary>
    /// <param name="folder">The folder.</param>
    public void ImportFolder(string folder, DateTime since)
    {
      List<string> availableFiles = new List<string>();
      Import(folder, ref availableFiles, since);

      if (availableFiles.Count > 0)
      {
        Build();
        foreach (string filename in availableFiles)
        {
          _importerInstance.FileImport(filename);
        }
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the deleted file.</param>
    public void FileDeleted(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        Build();
        _importerInstance.FileDeleted(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the new file.</param>
    public void FileCreated(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        Build();
        _importerInstance.FileCreated(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void FileChanged(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        Build();
        _importerInstance.FileChanged(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file or dikrectory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void FileRenamed(string filename, string oldFileName)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        Build();
        _importerInstance.FileRenamed(filename, oldFileName);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="file">The filename of the changed file.</param>
    public void DirectoryDeleted(string directoryname)
    {
      Build();
      _importerInstance.DirectoryDeleted(directoryname);
    }

    public void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items)
    {
      Build();
      _importerInstance.GetMetaDataFor(folder, ref items);
    }
    #endregion

    #region Private
    private void Build()
    {
      if (_importerInstance == null)
      {
        try
        {
          _importerInstance = (IImporter)_item.CreateObject(_item["class"]);
        }
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error(e.ToString() + "Can't create importer : " + _item.Id);
        }
      }
    }

    void Import(string folder, ref List<string> availableFiles, DateTime since)
    {
      //since = _lastImport;
      ServiceScope.Get<ILogger>().Info("Importer:{0} Importing:{1}", _item.Id, folder);
      try
      {
        string[] subFolders = Directory.GetDirectories(folder);
        for (int i = 0; i < subFolders.Length; ++i)
        {
          Import(subFolders[i], ref availableFiles, since);
        }

        string[] files = Directory.GetFiles(folder);
        for (int i = 0; i < files.Length; ++i)
        {
          string ext = Path.GetExtension(files[i]).ToLower();
          if (_extensions.Contains(ext))
          {
            if (CheckFile(files[i], since))
            {
              availableFiles.Add(files[i]);
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("Importer:{0}:error Importing:{1}", _item.Id, folder);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      //_lastImport = DateTime.Now;
    }

    bool CheckFile(string fileName, DateTime lastImport)
    {
      if ((File.GetAttributes(fileName) & FileAttributes.Hidden) == FileAttributes.Hidden)
      {
        return false;
      }
      if (File.GetCreationTime(fileName) > lastImport || File.GetLastWriteTime(fileName) > lastImport)
      {
        return true;
      }
      return false;
    }
    #endregion
  }
}
