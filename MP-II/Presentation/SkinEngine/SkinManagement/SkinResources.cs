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
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities.Files;
using Presentation.SkinEngine.MpfElements.Resources;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Encapsulates a collection of resources from a set of root directories.
  /// </summary>
  /// <remarks>
  /// Instances may represent resources from different directories. All directory contents
  /// are added in a defined precedence. All the directory contents are added to the collection
  /// of resource files.
  /// It is possible for a directory of a higher precedence to replace contents of directories
  /// of lower precedence.
  /// This class doesn't provide a sort of <i>reload</i> method, because to correctly
  /// reload all resources, we would have to check again all root directories. This is not the
  /// job of this class, as it only manages the contents of the root directories which were given to it.
  /// To avoid heavy load times at startup, this class will collect its resource files
  /// only when requested (lazy initializing).
  /// When the resources of this instance are no longer needed, method <see cref="Release()"/>
  /// can be called to reduce the memory consumption of this class.
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
    /// Lazy initialized resource file dictionary.
    /// </summary>
    /// <remarks>
    /// Holds all known resource files from our resource directories, stored in
    /// a dictionary: The key is the unified resource file name
    /// (relative path name starting at the beginning of the skinfile directory),
    /// the value is the <see cref="FileInfo"/> instance of the resource file.
    /// </remarks>
    protected IDictionary<string, FileInfo> _localResourceFiles = null;

    /// <summary>
    /// Lazy initialized style resources.
    /// </summary>
    protected ResourceDictionary _localStyleResources = null;
    protected SkinResources _inheritedSkinResources;

    // Meta information
    protected string _name;

    #endregion

    public SkinResources(string name, SkinResources inherited)
    {
      _name = name;
      _inheritedSkinResources = inherited;
    }

    /// <summary>
    /// Returns the information if this resources are ready to be used.
    /// </summary>
    public virtual bool IsValid
    {
      get { return true; }
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

    public object FindStyleResource(string resourceKey)
    {
      CheckStylesInitialized();
      if (_localStyleResources.ContainsKey(resourceKey))
        return _localStyleResources[resourceKey];
      // This code will also allow to use resources from the default skin, if
      // they are not implemented in the current theme/skin.
      // If we wanted strictly not to mix resources between themes, the next
      // if-block should be removed. This will avoid the fallback to our inherited resources.
      else if (_inheritedSkinResources != null)
        return _inheritedSkinResources.FindStyleResource(resourceKey);
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
        // We need to avoid indirect recursive calls here. We need to initialize our _localStyleResources before
        // loading the style resource files, because the elements in the resource files sometimes also access
        // style resources from lower priority skin resource styles.
        // Setting _localStyleResources to an empty ResourceDictionary here will avoid the repeated call of this method.
        _localStyleResources = new ResourceDictionary();
        foreach (KeyValuePair<string, FileInfo> resource in _localResourceFiles)
        {
          if (resource.Key.StartsWith(STYLES_DIRECTORY))
          {
            ResourceDictionary rd = XamlLoader.Load(resource.Value) as ResourceDictionary;
            if (rd == null)
              throw new InvalidCastException("Style resource file '" + resource.Value.ToString() +
                  "' doesn't contain a ResourceDictionary");
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

    /// <summary>
    /// Helper method to check the given version string to be equal or greater than the specified version number.
    /// </summary>
    protected static void CheckVersion(string versionStr, int expectedHigh, int expectedLow)
    {
      string[] numbers = versionStr.Split(new char[] { '.' });
      int verMax = Int32.Parse(numbers[0]);
      int verMin = 0;
      if (numbers.Length > 1)
        verMin = Int32.Parse(numbers[1]);
      if (numbers.Length > 2)
        throw new ArgumentException("Illegal version number '" + versionStr + "', expected format: '#.#'");
      if (verMax >= expectedHigh)
        return;
      if (verMin >= expectedLow)
        return;
      throw new ArgumentException("Version number '" + versionStr +
          "' is too low, at least '" + expectedHigh + "." + expectedLow + "' is needed");
    }
  }
}
