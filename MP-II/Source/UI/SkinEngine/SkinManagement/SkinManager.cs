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

namespace MediaPortal.SkinEngine.SkinManagement
{
  /// <summary>
  /// Manager class which caches all skins which are available in the system.
  /// </summary>
  public class SkinManager
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

      public bool RequestEnd(PluginItemMetadata item)
      {
        return true;
      }

      public void Stop(PluginItemMetadata item)
      {
        _skinManager.ReloadSkins();
      }

      public void Continue(PluginItemMetadata item) { }
    }

    public const string DEFAULT_SKIN = "default";

    #region Variables

    protected IDictionary<string, Skin> _skins = new Dictionary<string, Skin>();
    protected SkinResourcesPluginItemStateTracker _skinResourcesPluginItemStateTracker;

    #endregion

    public SkinManager()
    {
      _skinResourcesPluginItemStateTracker = new SkinResourcesPluginItemStateTracker(this);
      ReloadSkins();
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

    public void ReleaseSkinResources()
    {
      foreach (Skin skin in _skins.Values)
        skin.Release();
    }

    /// <summary>
    /// Will reload all skin information from the file system.
    /// </summary>
    public void ReloadSkins()
    {
      // We won't clear the skins so we don't loose our object references to the skins
      foreach (Skin skin in _skins.Values)
        skin.Release();

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
              if (_skins.ContainsKey(skinName))
                skin = _skins[skinName];
              else
                skin = _skins[skinName] = new Skin(skinName);
              skin.AddRootDirectory(skinDirectoryPath);
            }
          }
          catch (Exception e)
          {
            ServiceScope.Get<ILogger>().Warn("Error loading skins from directory '{0}'", e, rootDirectoryPath);
          }
        else
          ServiceScope.Get<ILogger>().Warn("Skin resource directory '{0}' doesn't exist", rootDirectoryPath);
      // Setup the resource chain: Inherit the default theme resources for all
      // skins other than the default skin
      Skin defaultSkin = DefaultSkin;
      SkinResources inheritedResources = defaultSkin == null ? null : defaultSkin.DefaultTheme;
      if (inheritedResources == null)
        inheritedResources = defaultSkin;
      foreach (KeyValuePair<string, Skin> kvp in _skins)
        if (kvp.Value != defaultSkin)
          kvp.Value.InheritedSkinResources = inheritedResources;
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
      else
        return null;
    }

    /// <summary>
    /// Returns all relevant skin root directories available in the system.
    /// </summary>
    /// <returns></returns>
    protected ICollection<string> GetSkinRootDirectoryPaths()
    {
      ICollection<string> result = new List<string>();
      IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
      foreach (PluginResource skinDirectoryResource in pluginManager.RequestAllPluginItems<PluginResource>(
          "/Resources/Skin", _skinResourcesPluginItemStateTracker))
        result.Add(skinDirectoryResource.Path);
      return result;
    }
  }
}
