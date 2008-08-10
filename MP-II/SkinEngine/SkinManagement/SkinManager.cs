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
using MediaPortal.Core.PathManager;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Manager class which caches all skins which are available in the system.
  /// </summary>
  public class SkinManager
  {
    public const string DEFAULT_SKIN = "default";

    #region Variables

    protected IDictionary<string, Skin> _skins = new Dictionary<string, Skin>();

    #endregion

    public SkinManager()
    {
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
      _skins.Clear();
      // Search application Skin directory
      DirectoryInfo skinDirectories = new DirectoryInfo(ServiceScope.Get<IPathManager>().GetPath("<SKIN>"));
      foreach (DirectoryInfo skinDirectory in skinDirectories.GetDirectories())
      {
        string skinName = skinDirectory.Name;
        Skin skin;
        if (_skins.ContainsKey(skinName))
          skin = _skins[skinName];
        else
          skin = _skins[skinName] = new Skin(skinDirectory.Name);
        skin.AddRootDirectory(skinDirectory);
      }
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
  }
}
