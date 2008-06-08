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

    public IDictionary<string, Skin> Skins
    {
      get { return _skins; }
    }

    public Skin DefaultSkin
    {
      get { return _skins[DEFAULT_SKIN]; }
    }

    /// <summary>
    /// Will reload all skin information from the system.
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
        skin.AddSkinDirectory(skinDirectory);
      }
    }

    /// <summary>
    /// Returns the skin with the specified name.
    /// </summary>
    /// <param name="skinName">Name of the skin to retrieve.</param>
    /// <returns><see cref="Skin"/> instance with the specified name, or <c>null</c> if the
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
