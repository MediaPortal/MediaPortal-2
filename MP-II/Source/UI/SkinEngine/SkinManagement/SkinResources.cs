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
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.SkinResources;
using MediaPortal.SkinEngine.MpfElements.Resources;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.SkinEngine.SkinManagement
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
  public class SkinResources: IResourceAccessor
  {
    public const string STYLES_DIRECTORY = "styles";
    public const string SCREENFILES_DIRECTORY = "screenfiles";
    public const string FONTS_DIRECTORY = "fonts";
    public const string SHADERS_DIRECTORY = "shaders";
    public const string MEDIA_DIRECTORY = "media";
    public const string WORKFLOW_DIRECTORY = "workflow";

    protected enum LoadState
    {
      Pending,
      Loading,
    }

    protected class PendingResource
    {
      protected string _resourcePath;
      protected LoadState _loadState;

      public PendingResource(string resourcePath)
      {
        _resourcePath = resourcePath;
        _loadState = LoadState.Pending;
      }

      public string ResourcePath
      {
        get { return _resourcePath; }
      }

      public LoadState State
      {
        get { return _loadState; }
        set { _loadState = value; }
      }
    }

    #region Protected fields

    protected IList<string> _rootDirectoryPaths = new List<string>();

    /// <summary>
    /// Lazy initialized resource file dictionary.
    /// </summary>
    /// <remarks>
    /// Holds all known resource files from our resource directories, stored in
    /// a dictionary: The key is the unified resource file name
    /// (relative path name starting at the beginning of the skinfile directory),
    /// the value is the absolute file path of the resource file.
    /// </remarks>
    protected IDictionary<string, string> _localResourceFilePaths = null;

    /// <summary>
    /// Lazy initialized style resources. Will contain the total of all style resources when
    /// the style loading has finished.
    /// </summary>
    protected ResourceDictionary _localStyleResources = null;

    /// <summary>
    /// Dictionary where we store the load state of all style resource files,
    /// for resolving style dependencies.
    /// </summary>
    protected IDictionary<string, PendingResource> _pendingStyleResources = null;

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

    public object FindStyleResource(object resourceKey)
    {
      CheckStylesInitialized();
      if (_localStyleResources.ContainsKey(resourceKey))
        return _localStyleResources[resourceKey];
      // This code will also allow to use resources from the default skin, if
      // they are not implemented in the current theme/skin.
      // If we wanted strictly not to mix resources between themes, the next
      // if-block should be removed. This will avoid the fallback to our inherited resources.
      if (_inheritedSkinResources != null)
        return _inheritedSkinResources.FindStyleResource(resourceKey);
      return null;
    }

    public string GetResourceFilePath(string resourceName)
    {
      return GetResourceFilePath(resourceName, true);
    }

    public string GetResourceFilePath(string resourceName, bool searchInheritedResources)
    {
      CheckResourcesInitialized();
      string key = resourceName.ToLower();
      if (_localResourceFilePaths.ContainsKey(key))
        return _localResourceFilePaths[key];
      if (searchInheritedResources && _inheritedSkinResources != null)
        return _inheritedSkinResources.GetResourceFilePath(resourceName);
      return null;
    }

    public IDictionary<string, string> GetResourceFilePaths(string regExPattern)
    {
      return GetResourceFilePaths(regExPattern, true);
    }

    public IDictionary<string, string> GetResourceFilePaths(string regExPattern, bool searchInheritedResources)
    {
      CheckResourcesInitialized();
      Dictionary<string, string> result = new Dictionary<string, string>();
      Regex regex = new Regex(regExPattern);
      foreach (KeyValuePair<string, string> kvp in _localResourceFilePaths)
        if (regex.IsMatch(kvp.Key))
          result.Add(kvp.Key, kvp.Value);
      if (searchInheritedResources && _inheritedSkinResources != null)
        foreach (KeyValuePair<string, string> kvp in _inheritedSkinResources.GetResourceFilePaths(regExPattern))
          if (!result.ContainsKey(kvp.Key))
            result.Add(kvp.Key, kvp.Value);
      return result;
    }

    /// <summary>
    /// Returns the skin file for the specified screen name.
    /// </summary>
    /// <param name="screenName">Logical name of the screen.</param>
    /// <returns>Absolute file path of the requested skin file.</returns>
    public string GetSkinFilePath(string screenName)
    {
      string key = SCREENFILES_DIRECTORY + Path.DirectorySeparatorChar + screenName + ".xaml";
      return GetResourceFilePath(key);
    }

    /// <summary>
    /// Loads the skin file with the specified name and returns its root element.
    /// </summary>
    /// <param name="screenName">Logical name of the screen.</param>
    /// <returns>Root element of the loaded skin or <c>null</c>, if the screen
    /// is not defined in this skin.</returns>
    public object LoadSkinFile(string screenName)
    {
      string skinFile = GetSkinFilePath(screenName);
      if (skinFile == null)
      {
        ServiceScope.Get<ILogger>().Error("SkinResources: No skinfile for screen '{0}'", screenName);
        return null;
      }
      return XamlLoader.Load(skinFile);
    }

    public void ClearRootDirectories()
    {
      _rootDirectoryPaths.Clear();
    }

    /// <summary>
    /// Adds a root directory with resource files to this resource collection.
    /// </summary>
    /// <param name="rootDirectoryPath">Directory containing files and subdirectories
    /// with resource files.</param>
    public void AddRootDirectory(string rootDirectoryPath)
    {
      Release();
      _rootDirectoryPaths.Add(rootDirectoryPath);
    }

    /// <summary>
    /// Releases all lazy initialized resources. This will reduce the memory consumption
    /// of this instance.
    /// When requested again, the skin resources will be loaded again automatically.
    /// </summary>
    public virtual void Release()
    {
      _localResourceFilePaths = null;
      _localStyleResources = null;
    }

    /// <summary>
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected virtual void CheckResourcesInitialized()
    {
      if (_localResourceFilePaths == null)
      {
        _localResourceFilePaths = new Dictionary<string, string>();
        foreach (string rootDirectoryPath in _rootDirectoryPaths)
          LoadDirectory(rootDirectoryPath);
      }
    }

    /// <summary>
    /// Tries to load a single style resource file. This method is for the special case
    /// that we are currently in the process of loading styles and while loading a style
    /// resource, another style resource is referenced.
    /// </summary>
    /// <remarks>
    /// If this method is called during the process of initializing the styles (like specified),
    /// it will prepone the loading of the style resource with the specified
    /// <paramref name="styleResourceName"/>. It will also detect cyclic dependencies.
    /// If it is called before the styles initialization, it will simply initialize all
    /// style resources.
    /// If called after the process of styles initialization, it will simply return
    /// (all style resources already have been initialized before).
    /// </remarks>
    internal void CheckStyleResourceWasLoaded(string styleResourceName)
    {
      string resourceKey = STYLES_DIRECTORY + "\\" + styleResourceName.ToLower() + ".xaml";
      if (_localStyleResources == null)
        // Method was called before the styles initialization
        CheckStylesInitialized();
      else if (_pendingStyleResources == null)
        // Method was called after the styles initialization has already finished
        return;
      else
      { // Do the actual work
        if (!_pendingStyleResources.ContainsKey(resourceKey))
        {
          if (_inheritedSkinResources != null)
            _inheritedSkinResources.CheckStyleResourceWasLoaded(resourceKey);
          return;
        }
        LoadStyleResource(resourceKey);
      }
      if (GetResourceFilePath(resourceKey) == null)
        ServiceScope.Get<ILogger>().Warn("SkinResources: Requested style resource '{0}' could not be found", resourceKey);
    }

    protected void LoadStyleResource(string resourceKey)
    {
      PendingResource pr;
      if (!_pendingStyleResources.TryGetValue(resourceKey, out pr))
        return;
      if (pr.State == LoadState.Loading)
        throw new CircularReferenceException(
            string.Format("Style resource '{0}' is part of a circular reference", resourceKey));
      pr.State = LoadState.Loading;
      try
      {
        ResourceDictionary rd = XamlLoader.Load(pr.ResourcePath) as ResourceDictionary;
        if (rd == null)
          throw new InvalidCastException("Style resource file '" + pr.ResourcePath +
              "' has to contain a ResourceDictionary as root element");
        _localStyleResources.Merge(rd);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("SkinResources: Error loading style resource '{0}'", ex, pr.ResourcePath);
      }
      finally
      {
        _pendingStyleResources.Remove(resourceKey);
      }
    }

    /// <summary>
    /// Will trigger the lazy initialization of styles on request.
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

        // Collect all style resources to be loaded
        _pendingStyleResources = new Dictionary<string, PendingResource>();
        foreach (KeyValuePair<string, string> resource in GetResourceFilePaths(
            "^" + STYLES_DIRECTORY + "\\\\.*\\.xaml$", false))
          _pendingStyleResources[resource.Key] = new PendingResource(resource.Value);
        // Load all pending resources. We use this complicated way because during the loading of
        // each style resource, another dependent resource might be requested to be loaded first.
        // Thats why we have to make use of this mixture of sequential and recursive
        // loading algorithm.
        KeyValuePair<string, PendingResource> kvp;
        while ((kvp = _pendingStyleResources.FirstOrDefault(
            kvpArg => kvpArg.Value.State == LoadState.Pending)).Key != null)
          LoadStyleResource(kvp.Key);
        _pendingStyleResources = null;
      }
    }

    protected virtual void LoadDirectory(string rootDirectoryPath)
    {
      // Add resource files for this directory
      int directoryNameLength = rootDirectoryPath.Length;
      foreach (string resourceFilePath in FileUtils.GetAllFilesRecursively(rootDirectoryPath))
      {
        string resourceName = resourceFilePath.Substring(directoryNameLength).ToLower();
        if (resourceName.StartsWith(Path.DirectorySeparatorChar.ToString()))
          resourceName = resourceName.Substring(1);
        if (_localResourceFilePaths.ContainsKey(resourceName))
          ServiceScope.Get<ILogger>().Warn("SkinResources: Duplicate skin resource '{0}', using resource from '{1}'",
              resourceName, _localResourceFilePaths[resourceName]);
        else
          _localResourceFilePaths[resourceName] = resourceFilePath;
      }
    }
  }
}
