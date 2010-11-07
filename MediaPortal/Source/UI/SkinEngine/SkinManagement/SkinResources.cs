#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.UI.SkinEngine.SkinManagement
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
  /// When the resources of this instance are no longer needed, method <see cref="Release"/>
  /// can be called to reduce the memory consumption of this instance.
  /// </remarks>
  public abstract class SkinResources: IResourceAccessor
  {
    #region Consts

    public const string STYLES_DIRECTORY = "styles";
    public const string SCREENS_DIRECTORY = "screens";
    public const string BACKGROUNDS_DIRECTORY = "backgrounds";
    public const string SUPER_LAYERS_DIRECTORY = "superlayers";
    public const string FONTS_DIRECTORY = "fonts";
    public const string SHADERS_DIRECTORY = "shaders";
    public const string MEDIA_DIRECTORY = "media";
    public const string WORKFLOW_DIRECTORY = "workflow";

    public const string MODELS_REGISTRATION_LOCATION = "/Models";

    #endregion

    #region Enums and classes

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

    protected class StyleResourceModelLoader : IModelLoader
    {
      protected SkinResources _parent;
      public StyleResourceModelLoader(SkinResources parent)
      {
        _parent = parent;
      }

      public object GetOrLoadModel(Guid modelId)
      {
        return _parent.GetOrLoadGUIModel(modelId);
      }
    }

    protected class ModelItemStateTracker : IPluginItemStateTracker
    {
      #region Protected fields

      protected SkinResources _parent;

      #endregion

      #region Ctor

      public ModelItemStateTracker(SkinResources parent)
      {
        _parent = parent;
      }

      #endregion

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "SkinResources: Usage of model in style resources"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        return !_parent.StyleGUIModels.ContainsKey(new Guid(itemRegistration.Metadata.Id));
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _parent.Release();
      }

      public void Continue(PluginItemRegistration itemRegistration) { }

      #endregion
    }

    #endregion

    #region Protected fields

    protected IList<string> _rootDirectoryPaths = new List<string>();

    /// <summary>
    /// Lazy initialized resource file dictionary.
    /// </summary>
    /// <remarks>
    /// Holds all known resource files from our resource directories, stored in
    /// a dictionary: The key is the unified resource file name
    /// (relative path name starting at the beginning of the skinfile directory in lower case invariant),
    /// the value is the absolute file path of the resource file in native case.
    /// </remarks>
    protected IDictionary<string, string> _localResourceFilePaths = null;

    /// <summary>
    /// Dictionary of style resources. Will contain the total of all style resources when
    /// the style loading has finished.
    /// </summary>
    protected ResourceDictionary _localStyleResources = null;

    /// <summary>
    /// Dictionary where we store the load state of all style resource files,
    /// for resolving style dependencies.
    /// </summary>
    protected IDictionary<string, PendingResource> _pendingStyleResources = null;

    protected SkinResources _inheritedSkinResources = null;

    // Meta information
    protected string _name;

    /// <summary>
    /// We request GUI models for our style resources - this plugin item tracker is present for
    /// those models.
    /// </summary>
    protected ModelItemStateTracker _modelItemStateTracker;

    /// <summary>
    /// Models currently loaded for the style.
    /// </summary>
    protected IDictionary<Guid, object> _styleGUIModels = new Dictionary<Guid, object>();

    #endregion

    protected SkinResources(string name)
    {
      _name = name;
      _modelItemStateTracker = new ModelItemStateTracker(this);
    }

    /// <summary>
    /// Returns the information if this resources are ready to be used.
    /// </summary>
    public virtual bool IsValid
    {
      get { return true; }
    }

    /// <summary>
    /// Gets the name of this resource bundle (skin name or theme name).
    /// </summary>
    public string Name
    {
      get { return _name; }
    }

    /// <summary>
    /// Gets the native width of the skin this resource bundle belongs to.
    /// </summary>
    public abstract int SkinWidth { get; }

    /// <summary>
    /// Gets the native height of the skin this resource bundle belongs to.
    /// </summary>
    public abstract int SkinHeight { get; }

    public IDictionary<Guid, object> StyleGUIModels
    {
      get { return _styleGUIModels; }
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

    protected bool IsResourcesInitialized
    {
      get { return _localResourceFilePaths != null; }
    }

    protected bool IsStylesInitialized
    {
      get { return _localStyleResources != null; }
    }

    public object FindStyleResource(object resourceKey)
    {
      if (!IsStylesInitialized)
        throw new IllegalCallException("SkinResources '{0}' were not prepared", this);
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
      SkinResources resourceBundle;
      return GetResourceFilePath(resourceName, true, out resourceBundle);
    }

    public string GetResourceFilePath(string resourceName, bool searchInheritedResources,
        out SkinResources resourceBundle)
    {
      if (resourceName == null)
      {
        resourceBundle = null;
        return null;
      }
      CheckResourcesInitialized();
      string key = resourceName.ToLowerInvariant();
      if (_localResourceFilePaths.ContainsKey(key))
      {
        resourceBundle = this;
        return _localResourceFilePaths[key];
      }
      if (searchInheritedResources && _inheritedSkinResources != null)
        return _inheritedSkinResources.GetResourceFilePath(resourceName, true, out resourceBundle);
      resourceBundle = null;
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
    /// <remarks>
    /// To invoke this method, use method <see cref="SkinManager.ReleaseSkinResources"/>.
    /// </remarks>
    internal virtual void Release()
    {
      _localResourceFilePaths = null;
      if (_localStyleResources != null)
        foreach (object resource in _localStyleResources.Values)
          if (resource is IDisposable)
            ((IDisposable) resource).Dispose();
      _localStyleResources = null;
      ReleaseAllGUIModels();
    }

    /// <summary>
    /// Prepares the resource chain. This method has to be called at the parent resource of the resource chain.
    /// </summary>
    /// <remarks>
    /// To prepare the skin resource chain, call method <see cref="SkinManager.InstallSkinResources"/>.
    /// </remarks>
    internal void Prepare()
    {
      SkinContext.SkinResources = this;
      InitializeStyleResourceLoading();
      LoadAllStyleResources();
    }

    protected object GetOrLoadGUIModel(Guid modelId)
    {
      object result;
      if (_styleGUIModels.TryGetValue(modelId, out result))
        return result;
      result = ServiceRegistration.Get<IPluginManager>().RequestPluginItem<object>(
          MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      if (result == null)
        throw new ArgumentException(string.Format("StyleResources: Model with id '{0}' is not available", modelId));
      _styleGUIModels[modelId] = result;
      return result;
    }

    protected void ReleaseAllGUIModels()
    {
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (Guid modelId in _styleGUIModels.Keys)
        pluginManager.RevokePluginItem(MODELS_REGISTRATION_LOCATION, modelId.ToString(), _modelItemStateTracker);
      _styleGUIModels.Clear();
    }

    /// <summary>
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected virtual void CheckResourcesInitialized()
    {
      if (IsResourcesInitialized)
        return;
      _localResourceFilePaths = new Dictionary<string, string>();
      foreach (string rootDirectoryPath in _rootDirectoryPaths)
        LoadDirectory(rootDirectoryPath);
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
    internal void CheckStyleResourceFileWasLoaded(string styleResourceName)
    {
      string resourceKey = STYLES_DIRECTORY + "\\" + styleResourceName.ToLowerInvariant() + ".xaml";
      if (_localStyleResources == null)
        // Method was called before the styles initialization
        throw new IllegalCallException("SkinResources '{0}' were not prepared", this);
      if (_pendingStyleResources == null)
        // Method was called after the styles initialization has already finished
        return;
      // Do the actual work
      LoadStyleResource(resourceKey, true);
      if (GetResourceFilePath(resourceKey) == null)
        ServiceRegistration.Get<ILogger>().Warn("SkinResources: Requested style resource '{0}' could not be found", resourceKey);
    }

    protected void LoadStyleResource(string resourceKey, bool searchInheritedResources)
    {
      PendingResource pr;
      if (_pendingStyleResources.TryGetValue(resourceKey, out pr))
      {
        if (pr.State != LoadState.Loading)
        {
          pr.State = LoadState.Loading;
          ILogger logger = ServiceRegistration.Get<ILogger>();
          try
          {
            logger.Info("SkinResources: Loading style resource '{0}' from file '{1}'", resourceKey, pr.ResourcePath);
            ResourceDictionary rd = XamlLoader.Load(pr.ResourcePath,
                new StyleResourceModelLoader(this), false) as ResourceDictionary;
            if (rd == null)
              throw new InvalidCastException("Style resource file '" + pr.ResourcePath +
                  "' doesn't contain a ResourceDictionary as root element");
            _localStyleResources.TakeOver(rd);
          }
          catch (Exception ex)
          {
            _pendingStyleResources.Clear();
            throw new EnvironmentException("Error loading style resource '{0}'", ex, pr.ResourcePath);
          }
          finally
          {
            _pendingStyleResources.Remove(resourceKey);
          }
        }
      }
      // Search in inherited resources after we searched through our own resources to make it possible to
      // override style resources in a file with the same name (in that case, the current file from our own resource collection
      // will be loaded first and thus is able to insert an overridden style before the inherited resource file is loaded).
      if (searchInheritedResources && _inheritedSkinResources != null)
        _inheritedSkinResources.LoadStyleResource(resourceKey, true);
    }

    /// <summary>
    /// Initializes the style resource loading process. Has to be called before
    /// <see cref="LoadAllStyleResources"/> is called.
    /// </summary>
    public void InitializeStyleResourceLoading()
    {
      // We need to initialize our _localStyleResources before loading the style resource files,
      // because the elements in the resource files sometimes also access style resources from lower
      // priority skin resource styles.
      // The opposite is also possible: a lower priority skin resource might depend on a style file which is
      // overridden here.
      // So the initialization is done in all resources of the style resource chain BEFORE
      // loading the dependency tree of resources.
      if (_inheritedSkinResources != null)
        _inheritedSkinResources.InitializeStyleResourceLoading();
      if (IsStylesInitialized)
        throw new IllegalCallException("SkinResources '{0}' are already prepared", this);
      CheckResourcesInitialized();
      _localStyleResources = new ResourceDictionary();

      // Collect all style resources to be loaded
      _pendingStyleResources = new Dictionary<string, PendingResource>();
      foreach (KeyValuePair<string, string> resource in GetResourceFilePaths(
          "^" + STYLES_DIRECTORY + "\\\\.*\\.xaml$", false))
        _pendingStyleResources[resource.Key] = new PendingResource(resource.Value);
    }

    /// <summary>
    /// Will trigger the actual initialization of styles. Before calling this method,
    /// the style loading has to be initialized by calling <see cref="InitializeStyleResourceLoading"/>.
    /// </summary>
    protected virtual void LoadAllStyleResources()
    {
      // Load all pending resources. We use this complicated way because during the loading of
      // each style resource, another dependent resource might be requested to be loaded first.
      // Thats why we have to make use of this mixture of sequential and recursive
      // loading algorithm.
      KeyValuePair<string, PendingResource> kvp;
      while ((kvp = _pendingStyleResources.FirstOrDefault(
          kvpArg => kvpArg.Value.State == LoadState.Pending)).Key != null)
        LoadStyleResource(kvp.Key, false);
      if (_inheritedSkinResources != null)
        _inheritedSkinResources.LoadAllStyleResources();
      _pendingStyleResources = null;
    }

    protected virtual void LoadDirectory(string rootDirectoryPath)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      logger.Info("SkinResources: Adding skin resource directory '{0}' to {1} '{2}'", rootDirectoryPath, GetType().Name, Name);
      // Add resource files for this directory
      int directoryNameLength = rootDirectoryPath.Length;
      foreach (string resourceFilePath in FileUtils.GetAllFilesRecursively(rootDirectoryPath))
      {
        string resourceName = resourceFilePath.Substring(directoryNameLength).ToLowerInvariant();
        if (resourceName.StartsWith(Path.DirectorySeparatorChar.ToString()))
          resourceName = resourceName.Substring(1);
        if (_localResourceFilePaths.ContainsKey(resourceName))
          logger.Warn("SkinResources: Duplicate skin resource '{0}', using resource from '{1}'",
              resourceName, _localResourceFilePaths[resourceName]);
        else
          _localResourceFilePaths[resourceName] = resourceFilePath;
      }
    }
  }
}
