#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Media.MediaManagement;

namespace Components.Services.ImporterManager
{
  /// <summary>
  /// Class which gets instantiated from the <c>Importer</c> builder to provide
  /// access to a lazy initialized importer instance.
  /// The plugin implementing the importer instance will be activated the first time the
  /// wrapped importer is accessed.
  /// </summary>
  internal class LazyImporterWrapper
  {
    #region Protected fields

    protected IList<string> _extensions;
    protected string _name;
    protected IImporter _importerInstance;
    protected string _importerClassName;
    protected PluginRuntime _plugin;

    #endregion

    #region Ctor

    public LazyImporterWrapper(string importerName, IList<string> extensions,
        string importerClassName, PluginRuntime plugin)
    {
      _name = importerName;
      _importerClassName = importerClassName;
      _extensions = extensions;
      for (int i = 0; i < _extensions.Count; i++)
        _extensions[i] = _extensions[i].ToLower();
      _plugin = plugin;
    }

    #endregion

    #region Importer wrapper methods

    /// <summary>
    /// Gets the importer name.
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets the file extensions this importer supports. The extensions are returned
    /// all in lower case.
    /// </summary>
    public IList<string> Extensions
    {
      get { return _extensions; }
    }

    /// <summary>
    /// Gets the instance of the importer. The instance will be lazy initialized on the first
    /// access.
    /// </summary>
    public IImporter Importer
    {
      get
      {
        if (_importerInstance == null)
          _importerInstance = (IImporter) _plugin.InstanciatePluginObject(_importerClassName);
        return _importerInstance;
      }
    }

    /// <summary>
    /// Called by the importer manager when a full-import needs to be done of the folder
    /// </summary>
    /// <param name="folder">The folder to import.</param>
    /// <param name="since">Only import files older than this value.</param>
    public void ImportFolder(string folder, DateTime since)
    {
      IList<string> availableFiles = new List<string>();
      CollectChangedFilesForImport(folder, availableFiles, since);

      if (availableFiles.Count <= 0) return;
      IImporter importer = Importer;
      importer.BeforeImport(availableFiles.Count);
      foreach (string filePath in availableFiles)
        importer.FileImport(filePath);
      importer.AfterImport();
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the deleted file.</param>
    public void FileDeleted(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
        Importer.FileDeleted(filename);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was created
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the new file.</param>
    public void FileCreated(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
        Importer.FileCreated(filename);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a file was changed
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="filename">The filename of the changed file.</param>
    public void FileChanged(string filename)
    {
      if (_extensions.Contains(Path.GetExtension(filename).ToLower()))
        Importer.FileChanged(filename);
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
        Importer.FileRenamed(filename, oldFileName);
    }

    /// <summary>
    /// Called by the importer manager after it detected that a directory was deleted
    /// This gives the importer a change to update the database
    /// </summary>
    /// <param name="directoryname">The directory name of the deleted directory.</param>
    public void DirectoryDeleted(string directoryname)
    {
      Importer.DirectoryDeleted(directoryname);
    }

    public void GetMetaDataFor(string folder, ref IList<IAbstractMediaItem> items)
    {
      Importer.GetMetaDataFor(folder, ref items);
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Collects all files in the specified <paramref name="folderPath"/> which
    /// can be imported by the wrapped importer (evaluated by the file extensions), which have
    /// been changed since the <paramref name="referenceTime"/>.
    /// </summary>
    /// <param name="folderPath">Path of the folder to examine.</param>
    /// <param name="availableFiles">Returns all files available for import in the specified
    /// <paramref name="folderPath"/>.</param>
    /// <param name="referenceTime">Time to compare the last file write times with. If a file
    /// was not changed since then, it won't be returned.</param>
    protected void CollectChangedFilesForImport(string folderPath, IList<string> availableFiles,
        DateTime referenceTime)
    {
      ServiceScope.Get<ILogger>().Info("Importer '{0}': Importing '{1}'", _name, folderPath);
      try
      {
        foreach (string subFolderPath in Directory.GetDirectories(folderPath))
          CollectChangedFilesForImport(subFolderPath, availableFiles, referenceTime);

        foreach (string filePath in Directory.GetFiles(folderPath))
        {
          if (CheckFile(filePath, referenceTime))
            availableFiles.Add(filePath);
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Importer '{0}': Error importing '{1}'", ex, _name, folderPath);
      }
    }

    /// <summary>
    /// Checks if the file with the specified <paramref name="filePath"/> meets the criteria to
    /// be imported. The checked criteria are:
    /// <list type="bullet">
    /// <item>The file extension must be contained in the extensions list for this importer.</item>
    /// <item>The file attributes mustn't include the "hidden" flag.</item>
    /// <item>The last write time of the file must be later than the <paramref name="referenceTime"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="filePath">The path to the file to be checked.</param>
    /// <param name="referenceTime">The time to be compared with the last write time of the file.</param>
    /// <returns><c>true</c>, if the given file meets the criteria to be imported again, else <c>false</c>.</returns>
    protected bool CheckFile(string filePath, DateTime referenceTime)
    {
      string ext = Path.GetExtension(filePath).ToLower();
      if (!_extensions.Contains(ext))
        return false;
      FileInfo fi = new FileInfo(filePath);
      if ((fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
        return false;
      if (fi.CreationTime > referenceTime || fi.LastWriteTime > referenceTime)
        return true;
      return false;
    }

    #endregion
  }
}
