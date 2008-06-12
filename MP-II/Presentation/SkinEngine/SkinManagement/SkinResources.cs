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
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Files;
using Presentation.SkinEngine.MpfElements.Resources;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Encapsulates a collection of resources from different root directories.
  /// </summary>
  /// <remarks>
  /// This class will lazy load its resources on the first access.
  /// </remarks>
  public class SkinResources
  {
    public const string STYLES_DIRECTORY = "styles";
    public const string SCREENFILES_DIRECTORY = "screenfiles";
    public const string FONTS_DIRECTORY = "fonts";
    public const string SHADERS_DIRECTORY = "shaders";
    public const string MEDIA_DIRECTORY = "media";

    #region Protected fields

    protected IList<DirectoryInfo> _rootDirectories = new List<DirectoryInfo>();

    /// <summary>
    /// Holds all known resource files from our resource directories, stored in
    /// a dictionary: The key is the unified resource file name
    /// (relative path name starting at the beginning of the skinfile directory),
    /// the value is the <see cref="FileInfo"/> instance of the resource file.
    /// </summary>
    protected IDictionary<string, FileInfo> _localResourceFiles = null;

    protected ResourceDictionary _localStyleResources;
    protected SkinResources _inheritedSkinResources;

    // Meta information
    protected string _name;

    #endregion

    public SkinResources(string name, SkinResources inherited)
    {
      _name = name;
      _inheritedSkinResources = inherited;
    }

    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets or sets the <see cref="SkinResources"/> instance which inherits
    /// its resources to this instance.
    /// </summary>
    public SkinResources InheritedSkinResources
    {
      get { return _inheritedSkinResources; }
      set { _inheritedSkinResources = value; }
    }

    public object FindStyle(string resourceKey)
    {
      CheckStylesInitialized();
      if (_localStyleResources.ContainsKey(resourceKey))
        return _localStyleResources[resourceKey];
      // This code will also allow to use resources from the default skin, if
      // they are not implemented in the current theme/skin.
      // If we wanted strictly not to mix resources between themes, the next
      // if-block should be removed. This will avoid the fallback to our inherited resources.
      else if (_inheritedSkinResources != null)
        return _inheritedSkinResources.FindStyle(resourceKey);
      else
        return null;
    }

    /// <summary>
    /// Returns the resource file for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the
    /// path of the resource relative to the root directory level of this resource
    /// collection directory.</param>
    /// <returns>System filename of the specified resource or <c>null</c> if
    /// the resource is not defined.</returns>
    public FileInfo GetResourceFile(string resourceName)
    {
      CheckResourcesInitialized();
      string key = resourceName.ToLower();
      if (_localResourceFiles.ContainsKey(key))
        return _localResourceFiles[key];
      else if (_inheritedSkinResources != null)
        return _inheritedSkinResources.GetResourceFile(resourceName);
      return null;
    }

    /// <summary>
    /// Returns all resource files in this resource collection, where their relative directory
    /// name match the specified regular expression pattern <paramref name="regExPattern"/>.
    /// </summary>
    /// <param name="regExPattern">Regular expression pattern which will be applied on the
    /// unified resource name.</param>
    /// <returns>Dictionary with a mapping of unified resource names to file infos of those
    /// resource files which match the search criterion.</returns>
    public IDictionary<string, FileInfo> GetResourceFiles(string regExPattern)
    {
      CheckResourcesInitialized();
      Dictionary<string, FileInfo> result = new Dictionary<string, FileInfo>();
      Regex regex = new Regex(regExPattern.ToLower());
      foreach (KeyValuePair<string, FileInfo> kvp in _localResourceFiles)
        if (regex.IsMatch(kvp.Key))
          result.Add(kvp.Key, kvp.Value);
      if (_inheritedSkinResources != null)
        foreach (KeyValuePair<string, FileInfo> kvp in _inheritedSkinResources.GetResourceFiles(regExPattern))
          if (!result.ContainsKey(kvp.Key))
            result.Add(kvp.Key, kvp.Value);
      return result;
    }

    /// <summary>
    /// Returns the skin file for the specified screen name.
    /// </summary>
    /// <param name="screenName">Logical name of the screen.</param>
    /// <returns></returns>
    public FileInfo GetSkinFile(string screenName)
    {
      string key = SCREENFILES_DIRECTORY + Path.DirectorySeparatorChar + screenName + ".xaml";
      return GetResourceFile(key);
    }

    /// <summary>
    /// Loads the skin file with the specified name and returns its root element.
    /// </summary>
    /// <param name="screenName">Logical name of the screen.</param>
    /// <returns>Root element of the loaded skin or <c>null</c>, if the screen
    /// is not defined in this skin.</returns>
    public object LoadSkinFile(string screenName)
    {
      FileInfo skinFile = GetSkinFile(screenName);
      if (skinFile == null)
        return null;
      return XamlLoader.Load(skinFile);
    }

    /// <summary>
    /// Adds a root directory with resource files to this resource collection.
    /// </summary>
    /// <param name="rootDirectory">Directory containing files and subdirectories
    /// with resource files.</param>
    public void AddRootDirectory(DirectoryInfo rootDirectory)
    {
      Release();
      _rootDirectories.Add(rootDirectory);
    }

    /// <summary>
    /// Releases all lazy initialized resources. This will reduce the memory consumption
    /// of this instance.
    /// When requested again, the skin resources will be loaded again automatically.
    /// </summary>
    public virtual void Release()
    {
      _localResourceFiles = null;
      _localStyleResources = null;
    }

    /// <summary>
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected virtual void CheckResourcesInitialized()
    {
      if (_localResourceFiles == null)
      {
        _localResourceFiles = new Dictionary<string, FileInfo>();
        foreach (DirectoryInfo rootDirectory in _rootDirectories)
          LoadDirectory(rootDirectory);
      }
    }

    /// <summary>
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected virtual void CheckStylesInitialized()
    {
      if (_localStyleResources == null)
      {
        CheckResourcesInitialized();
        // We need to avoid recursive calls here. We need to initialize _localStyleResources before
        // loading the style resource files, which themselves will perhaps request styles.
        _localStyleResources = new ResourceDictionary();
        foreach (KeyValuePair<string, FileInfo> resource in _localResourceFiles)
        {
          if (resource.Key.StartsWith(STYLES_DIRECTORY))
          {
            ResourceDictionary rd = XamlLoader.Load(resource.Value) as ResourceDictionary;
            _localStyleResources.Merge(rd);
          }
        }
      }
    }

    protected virtual void LoadDirectory(DirectoryInfo rootDirectory)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      // Add resource files for this directory
      int directoryNameLength = rootDirectory.FullName.Length;
      foreach (FileInfo resourceFile in FileUtils.GetAllFilesRecursively(rootDirectory))
      {
        string resourceName = resourceFile.FullName;
        resourceName = resourceName.Substring(directoryNameLength).ToLower();
        if (resourceName.StartsWith(Path.DirectorySeparatorChar.ToString()))
          resourceName = resourceName.Substring(1);
        if (_localResourceFiles.ContainsKey(resourceName))
          logger.Info("Duplicate resource file for resource collection '{0}': '{1}', '{2}'",
              _name, _localResourceFiles[resourceName].FullName, resourceName);
        else
          _localResourceFiles[resourceName] = resourceFile;
      }
    }
  }
}
