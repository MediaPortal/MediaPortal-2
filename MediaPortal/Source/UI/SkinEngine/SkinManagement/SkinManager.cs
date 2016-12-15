#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Common.Services.PluginManager;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;

namespace MediaPortal.UI.SkinEngine.SkinManagement
{
  /// <summary>
  /// Manager class which manages and caches all skins which are available in the system.
  /// This class also is responsible for the skin's background manager.
  /// </summary>
  public class SkinManager : ISkinResourceManager
  {
    public const string BACKGROUND_PLUGIN_ITEM_ID = "Background";

    /// <summary>
    /// Data object to store data about the current installed background manager.
    /// This class also implements <see cref="IPluginItemStateTracker"/> to track the background's plugin
    /// item state.
    /// </summary>
    protected class BackgroundManagerData : IPluginItemStateTracker
    {
      protected IBackgroundManager _backgroundManager = null;
      protected string _backgroundLocation = null;
      protected SkinManager _parent;

      public BackgroundManagerData(SkinManager parent)
      {
        _parent = parent;
      }

      public bool Install(Skin skin)
      {
        Uninstall();
        string location = "/Skins/" + skin.Name;
        IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
        PluginItemMetadata md = pluginManager.GetPluginItemMetadata(location, BACKGROUND_PLUGIN_ITEM_ID);
        if (md == null)
          return false;
        try
        {
          _backgroundLocation = location;
          _backgroundManager = pluginManager.RequestPluginItem<IBackgroundManager>(
                _backgroundLocation, BACKGROUND_PLUGIN_ITEM_ID, this);
          _backgroundManager.Install();
          return true;
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot install background manager for {0}", e, location);
        }
        return false;
      }

      public void Uninstall()
      {
        if (_backgroundManager == null)
          return;
        _backgroundManager.Uninstall();
        ServiceRegistration.Get<IPluginManager>().RevokePluginItem(_backgroundLocation, BACKGROUND_PLUGIN_ITEM_ID, this);
        _backgroundManager = null;
        _backgroundLocation = null;
      }

      public string BackgroundLocation
      {
        get { return _backgroundLocation; }
      }

      public IBackgroundManager BackgroundManager
      {
        get { return _backgroundManager; }
      }

      public bool IsInstalled
      {
        get { return _backgroundManager != null; }
      }

      #region IPluginItemStateTracker implementation

      public string UsageDescription
      {
        get { return "SkinManager: Usage of background manager"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        Uninstall();
      }

      public void Continue(PluginItemRegistration itemRegistration)
      {
      }

      #endregion
    }

    /// <summary>
    /// Plugin item registration path where skin resources are registered.
    /// </summary>
    public const string SKIN_RESOURCES_REGISTRATION_PATH = "/Resources/Skin";

    /// <summary>
    /// Name of the default (fallback) skin.
    /// </summary>
    public const string DEFAULT_SKIN = "default";

    #region Protected fields

    protected readonly IDictionary<string, Skin> _skins = new Dictionary<string, Skin>();
    protected readonly IPluginItemStateTracker _skinResourcesPluginItemStateTracker;
    protected readonly IItemRegistrationChangeListener _skinResourcesRegistrationChangeListener;
    protected readonly BackgroundManagerData _backgroundManagerData;

    #endregion

    public SkinManager()
    {
      _skinResourcesPluginItemStateTracker = new DefaultItemStateTracker("SkinManager: Usage of skin resources")
        {
            Stopped = itemRegistration => SkinResourcesWereChanged()
        };
      _skinResourcesRegistrationChangeListener = new DefaultItemRegistrationChangeListener("SkinManager: Usage of skin resources")
        {
            ItemsWereAdded = (location, items) => SkinResourcesWereChanged()
            // Item removals are handled by the plugin item state tracker
        };
      _backgroundManagerData = new BackgroundManagerData(this);
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      pluginManager.AddItemRegistrationChangeListener(
          SKIN_RESOURCES_REGISTRATION_PATH, _skinResourcesRegistrationChangeListener);
      ReloadSkins();
    }

    public void Dispose()
    {
      UninstallBackgroundManager();
      ReleasePluginSkinResources();
      ReleaseSkinResources();
      _skins.Clear();
    }

    /// <summary>
    /// Returns all available skins which were discovered by the skin manager.
    /// </summary>
    /// <remarks>
    /// Be careful: The returned skins may contain <see cref="Skin"/> instances which are not 
    /// <see cref="Skin.IsValid">valid</see>.
    /// </remarks>
    public IDictionary<string, Skin> Skins
    {
      get { return _skins; }
    }

    public Skin DefaultSkin
    {
      get { return _skins.ContainsKey(DEFAULT_SKIN) ? _skins[DEFAULT_SKIN] : null; }
    }

    public void InstallSkinResources(SkinResources skinResources)
    {
      // Setup the resource chain and use the default skin as last fallback
      skinResources.SetupResourceChain(_skins, DefaultSkin);
      // Initialize SkinContext with new resource bundle
      SkinContext.SkinResources = skinResources;
      skinResources.InitializeStyleResourceLoading(); // Initializes the resource file dictionary
      Fonts.FontManager.Load(skinResources); // Needs an initialized resource file dictionary - loads font files
      skinResources.LoadAllStyleResources(); // Needs the FontManager
      // Notify others that we loaded a new skin or theme
      SkinResourcesMessaging.SendSkinResourcesMessage(SkinResourcesMessaging.MessageType.SkinOrThemeChanged);
    }

    /// <summary>
    /// Reduces memory consumption by releasing all cached data for all skins. The caches will be
    /// re-filled again on demand.
    /// </summary>
    public void ReleaseSkinResources()
    {
      foreach (Skin skin in _skins.Values)
        skin.Release();
    }

    /// <summary>
    /// Releases all plugin items requested to get the skin directories.
    /// </summary>
    protected void ReleasePluginSkinResources()
    {
      ServiceRegistration.Get<IPluginManager>().RevokeAllPluginItems(
          SKIN_RESOURCES_REGISTRATION_PATH, _skinResourcesPluginItemStateTracker);
    }

    /// <summary>
    /// Will reload all skins from the file system.
    /// </summary>
    public void ReloadSkins()
    {
      // We won't clear the skins so we don't loose our object references to the skins
      foreach (Skin skin in _skins.Values)
      {
        skin.Release();
        skin.ClearRootDirectories();
      }

      foreach (string rootDirectoryPath in GetSkinRootDirectoryPaths())
        if (Directory.Exists(rootDirectoryPath))
          try
          {
            foreach (string skinDirectoryPath in Directory.GetDirectories(rootDirectoryPath))
            {
              string skinName = Path.GetFileName(skinDirectoryPath) ?? string.Empty;
              if (skinName.StartsWith("."))
                continue;
              Skin skin;
              if (!_skins.TryGetValue(skinName, out skin))
                skin = _skins[skinName] = new Skin(skinName);
              skin.AddRootDirectory(skinDirectoryPath);
            }
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("SkinManager: Error loading skins from directory '{0}'", e, rootDirectoryPath);
          }
        else
          ServiceRegistration.Get<ILogger>().Warn("SkinManager: Skin resource directory '{0}' doesn't exist", rootDirectoryPath);
    }

    /// <summary>
    /// Returns the skin with the specified name.
    /// </summary>
    /// <param name="skinName">Name of the skin to retrieve.</param>
    /// <returns><see cref="SkinManagement.Skin"/> instance with the specified name, or <c>null</c> if the
    /// skin could not be found.</returns>
    public Skin GetSkin(string skinName)
    {
      if (_skins.ContainsKey(skinName))
        return _skins[skinName];
      return null;
    }

    public bool InstallBackgroundManager(Skin skin)
    {
      // Loading and disposing of the background manager will be handled by the BackgroundManagerData class
      UninstallBackgroundManager();
      SkinResources current = skin;
      while (current != null)
      {
        if (current is Skin && _backgroundManagerData.Install((Skin) current))
          return true;
        current = current.InheritedSkinResources;
      }
      return false;
    }

    public void UninstallBackgroundManager()
    {
      _backgroundManagerData.Uninstall();
    }

    protected void SkinResourcesWereChanged()
    {
      ReloadSkins();
      SkinResourcesMessaging.SendSkinResourcesMessage(SkinResourcesMessaging.MessageType.SkinResourcesChanged);
    }

    /// <summary>
    /// Returns all relevant skin root directories available in the system.
    /// </summary>
    /// <returns>Collection of skin root directories.</returns>
    protected ICollection<string> GetSkinRootDirectoryPaths()
    {
      ReleasePluginSkinResources();
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      return pluginManager.RequestAllPluginItems<PluginResource>(SKIN_RESOURCES_REGISTRATION_PATH, _skinResourcesPluginItemStateTracker).Select(
          skinDirectoryResource => skinDirectoryResource.Path).ToList();
    }

    #region ISkinResourceManager implementation

    public IResourceAccessor SkinResourceContext
    {
      get { return SkinContext.SkinResources; }
    }

    #endregion
  }
}
