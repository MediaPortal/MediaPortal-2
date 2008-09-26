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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Media.Importers;
using MediaPortal.Media.MediaManager;

namespace Components.Services.Importers
{
  class ImporterBuilder : IPluginItemBuilder
  {
    #region variables
    string _type;
    string _extensions;
    string _name;
    IImporter _importerInstance;
    #endregion

    #region IPluginItemBuilder methods

    public object BuildItem(PluginItemMetadata itemData, PluginRuntime plugin)
    {
      ImporterBuilder builder = new ImporterBuilder();

      if (itemData.Attributes.ContainsKey("Extensions"))
      {
        builder._extensions = itemData.Attributes["Extensions"];
      }

      if (itemData.Attributes.ContainsKey("Type"))
      {
        builder._type = itemData.Attributes["Type"];
      }

      builder._importerInstance = (IImporter) plugin.InstanciatePluginObject(itemData.Attributes["ClassName"]);

      builder._name = itemData.Id;

      return builder;
    }

    public bool NeedsPluginActive
    {
      get { return true; }
    }

    #endregion

    #region IImporterBuilder

    /// <summary>
    /// Gets the importer name.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets the file-extensions the importer supports.
    /// </summary>
    public string Extensions
    {
      get { return _extensions; }
    }

    /// <summary>
    /// Gets the instance of the importer.
    /// </summary>
    public IImporter Importer
    {
      get { return _importerInstance; }
    }

    /// <summary>
    /// Called by the importer manager when a full-import needs to be done from the folder
    /// </summary>
    /// <param name="folder">The folder to import.</param>
    /// <param name="since">Only import files older than this value.</param>
    public void ImportFolder(string folder, DateTime since)
    {
      List<string> availableFiles = new List<string>();
      Import(folder, ref availableFiles, since);

      if (availableFiles.Count > 0)
      {
        _importerInstance.BeforeImport(availableFiles.Count);
        foreach (string filename in availableFiles)
        {
          _importerInstance.FileImport(filename);
        }
        _importerInstance.AfterImport();
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the deleted file.</param>
    public void FileDeleted(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        _importerInstance.FileDeleted(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the new file.</param>
    public void FileCreated(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        _importerInstance.FileCreated(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the changed file.</param>
    public void FileChanged(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        _importerInstance.FileChanged(filename);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file or directory was renamed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The new filename of the changed file.</param>
    /// <param name="oldFileName">The old filename of the changed file.</param>
    public void FileRenamed(string filename, string oldFileName)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
      {
        _importerInstance.FileRenamed(filename, oldFileName);
      }
    }

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="directoryname">The directory name of the deleted directory.</param>
    public void DirectoryDeleted(string directoryname)
    {
      _importerInstance.DirectoryDeleted(directoryname);
    }

    public void GetMetaDataFor(string folder, ref List<IAbstractMediaItem> items)
    {
      _importerInstance.GetMetaDataFor(folder, ref items);
    }
    #endregion

    #region Private

    void Import(string folder, ref List<string> availableFiles, DateTime since)
    {
      //since = _lastImport;
      ServiceScope.Get<ILogger>().Info("Importer '{0}': Importing '{1}'", _name, folder);
      try
      {
        foreach (string subFolderPath in Directory.GetDirectories(folder))
          Import(subFolderPath, ref availableFiles, since);

        foreach (string filePath in Directory.GetFiles(folder))
        {
          string ext = Path.GetExtension(filePath).ToLower();
          if (_extensions.Contains(ext))
            if (CheckFile(filePath, since))
              availableFiles.Add(filePath);
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Info("Importer '{0}': Error importing '{1}'", _name, folder);
        ServiceScope.Get<ILogger>().Error(ex);
      }
      //_lastImport = DateTime.Now;
    }

    static bool CheckFile(string filePath, DateTime lastImport)
    {
      FileInfo fi = new FileInfo(filePath);
      if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
        return false;
      if (fi.CreationTime > lastImport || fi.LastWriteTime > lastImport)
        return true;
      return false;
    }
    #endregion
  }
}
