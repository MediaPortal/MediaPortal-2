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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Services.PluginManager;
using MediaPortal.Presentation.SkinResources;

namespace MediaPortal.SkinEngine.SkinManagement
{
  /// <summary>
  /// Manager class which caches all skins which are available in the system.
  /// </summary>
  public class SkinManager : ISkinResourceManager
  {
    /// <summary>
    /// Plugin item state tracker which allows skin resources to be revoked. When skin resources
    /// are revoked by the plugin manager, the <see cref="SkinManager.ReloadSkins()"/> method
    /// will be called from the <see cref="SkinResourcesPluginItemStateTracker.Stop"/> method.
    /// </summary>
    protected class SkinResourcesPluginItemStateTracker: IPluginItemStateTracker
    {
      protected SkinManager _skinManager;

      public SkinResourcesPluginItemStateTracker(SkinManager skinManager)
      {
        _skinManager = skinManager;
      }

      public string UsageDescription
      {
        get { return "SkinManager: Usage of skin resources"; }
      }

      public bool RequestEnd(PluginItemRegistration itemRegistration)
      {
        return true;
      }

      public void Stop(PluginItemRegistration itemRegistration)
      {
        _skinManager.SkinResourcesWereChanged();
      }

      public void Continue(PluginItemRegistration itemRegistration) { }
    }

    protected class SkinResourcesRegistrationChangeListener: IItemRegistrationChangeListener
    {
      protected SkinManager _skinManager;

      public SkinResourcesRegistrationChangeListener(SkinManager skinManager)
      {
        _skinManager = skinManager;
      }

      #region IItemRegistrationChangeListener implementation

      public void ItemsWereAdded(string location, ICollection<PluginItemMetadata> items)
      {
        _skinManager.SkinResourcesWereChanged();
      }

      public void ItemsWereRemoved(string location, ICollection<PluginItemMetadata> items)
      {
        // Item removals are handeled by the SkinResourcesPluginItemStateTracker
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

    protected IDictionary<string, Skin> _skins = new Dictionary<string, Skin>();
    protected SkinResourcesPluginItemStateTracker _skinResourcesPluginItemStateTracker;
    protected SkinResourcesRegistrationChangeListener _skinResourcesRegistrationChangeListener;

    #endregion

    public SkinManager()
    {
      _skinResourcesPluginItemStateTracker = new SkinResourcesPluginItemStateTracker(this);
      _skinResourcesRegistrationChangeListener = new SkinResourcesRegistrationChangeListener(this);
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      pluginManager.AddItemRegistrationChangeListener(
          SKIN_RESOURCES_REGISTRATION_PATH, _skinResourcesRegistrationChangeListener);
      ReloadSkins();
    }

    public void Dispose()
    {
      ReleasePluginSkinResources();
      _skins = null;
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
      // Initialize SkinContext with new values
      skinResources.Prepare();
      Theme theme;
      Skin skin;
      if (skinResources is Theme)
      {
        theme = (Theme) skinResources;
        skin = theme.ParentSkin;
      }
      else if (skinResources is Skin)
      {
        theme = null;
        skin = (Skin) skinResources;
      }
      else
        throw new ArgumentException("The specified skin resources '{0}' cannot be installed; supported types are Skin and Theme");

      SkinContext.SkinName = skin.Name;
      SkinContext.SkinHeight = skin.NativeHeight;
      SkinContext.SkinWidth = skin.NativeWidth;
      SkinContext.ThemeName = theme == null ? null : theme.Name;
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
      ServiceScope.Get<IPluginManager>().RevokeAllPluginItems(
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
              string skinName = Path.GetFileName(skinDirectoryPath);
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
            ServiceScope.Get<ILogger>().Warn("SkinManager: Error loading skins from directory '{0}'", e, rootDirectoryPath);
          }
        else
          ServiceScope.Get<ILogger>().Warn("SkinManager: Skin resource directory '{0}' doesn't exist", rootDirectoryPath);
      // Setup the resource chain: Inherit the theme resources of the based-on-skin for all
      // skins, and use the default skin as last fallback
      Skin defaultSkin = DefaultSkin;
      SkinResources defaultInheritResources = (defaultSkin == null ? null : defaultSkin.DefaultTheme) ??
          (SkinResources) defaultSkin;
      foreach (KeyValuePair<string, Skin> kvp in _skins)
      {
        Skin current = kvp.Value;
        Skin basedOnSkin;
        SkinResources inheritResources;
        if (current.BasedOnSkin != null && _skins.TryGetValue(current.BasedOnSkin, out basedOnSkin))
          inheritResources = basedOnSkin.DefaultTheme ?? (SkinResources) basedOnSkin;
        else
          inheritResources = kvp.Key == DEFAULT_SKIN ? null : defaultInheritResources;
        current.InheritedSkinResources = inheritResources;
      }
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

    protected void SkinResourcesWereChanged()
    {
      ReloadSkins();
      if (SkinResourcesChanged != null)
        SkinResourcesChanged();
    }

    /// <summary>
    /// Returns all relevant skin root directories available in the system.
    /// </summary>
    /// <returns>Collection of skin root directories.</returns>
    protected ICollection<string> GetSkinRootDirectoryPaths()
    {
      ReleasePluginSkinResources();
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      ICollection<string> result = new List<string>();
      foreach (PluginResource skinDirectoryResource in pluginManager.RequestAllPluginItems<PluginResource>(
          SKIN_RESOURCES_REGISTRATION_PATH, _skinResourcesPluginItemStateTracker))
        result.Add(skinDirectoryResource.Path);
      return result;
    }

    #region ISkinResourceManager implementation

    public IResourceAccessor SkinResourceContext
    {
      get { return SkinContext.SkinResources; }
    }

    public event SkinResourceCollectionChangedDlgt SkinResourcesChanged;

    #endregion
  }
}
