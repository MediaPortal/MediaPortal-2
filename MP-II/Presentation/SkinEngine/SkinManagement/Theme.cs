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

using System.Collections.Generic;
using System.IO;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Files;
using Presentation.SkinEngine.MpfElements.Resources;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Holds theme files for its <see cref="ParentSkin"/>.
  /// </summary>
  /// <remarks>
  /// This class will eager load its resources.
  /// </remarks>
  public class Theme
  {
    public const string STYLES_DIRECTORY = "styles";

    #region Variables

    /// <summary>
    /// Holds all known resource files in this skin, stored as a dictionary: The key is the unified
    /// resource file name (relative path name starting at the beginning of the skinfile directory),
    /// the value is the <see cref="FileInfo"/> instance of the resource file.
    /// </summary>
    protected IDictionary<string, FileInfo> _resourceFiles = new Dictionary<string, FileInfo>();

    // Meta information
    protected string _name;
    protected Skin _parentSkin;

    #endregion

    public Theme(string name, Skin parentSkin)
    {
      _name = name;
      _parentSkin = parentSkin;
    }

    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Returns the <see cref="Skin"/> this theme belongs to.
    /// </summary>
    public Skin ParentSkin
    {
      get { return _parentSkin; }
    }

    /// <summary>
    /// Returns the resource file for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the
    /// path of the resource relative to the skin directory.</param>
    /// <returns></returns>
    public FileInfo GetResourceFile(string resourceName)
    {
      if (_resourceFiles.ContainsKey(resourceName))
        return _resourceFiles[resourceName];
      else
        return null;
    }

    /// <summary>
    /// Loads all styles <see cref="ResourceDirectory"/>s stored in this theme.
    /// </summary>
    /// <returns>Resource dictionary containing all style resources of this theme.</returns>
    public ResourceDictionary LoadStyles()
    {
      ResourceDictionary result = new ResourceDictionary();
      foreach (KeyValuePair<string, FileInfo> resource in _resourceFiles)
      {
        if (resource.Key.StartsWith(STYLES_DIRECTORY))
        {
          ResourceDictionary rd = XamlLoader.Load(resource.Value) as ResourceDictionary;
          result.Merge(rd);
        }
      }
      return result;
    }

    /// <summary>
    /// Adds a directory with search information for this theme.
    /// </summary>
    /// <param name="themeDirectory">Directory containing theme information. The
    /// specified directory should be one of the root directories for this theme.</param>
    public void AddThemeDirectory(DirectoryInfo themeDirectory)
    {
      LoadDirectory(themeDirectory);
    }

    protected void LoadDirectory(DirectoryInfo themeDirectory)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      // Add resource files for this directory
      int directoryNameLength = themeDirectory.FullName.Length;
      foreach (FileInfo resourceFile in FileUtils.GetAllFilesRecursively(themeDirectory))
      {
        string resourceName = resourceFile.FullName;
        resourceName = resourceName.Substring(directoryNameLength);
        if (resourceName.StartsWith(Path.DirectorySeparatorChar.ToString()))
          resourceName = resourceName.Substring(1);
        if (_resourceFiles.ContainsKey(resourceName))
          logger.Info("Duplicate resource file for skin '{0}': '{1}', '{2}'", _name, _resourceFiles[resourceName].FullName, resourceName);
        else
          _resourceFiles[resourceName] = resourceFile;
      }
    }
  }
}
