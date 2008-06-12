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

using System.IO;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace Presentation.SkinEngine.SkinManagement
{
  /// <summary>
  /// Represents a skin - which is a logical construct consisting of a logical name
  /// (skin name), some meta information (like the preferred size and the preview image)
  /// and a set of skin files.
  /// This class provides methods for requesting the meta information, available skin files,
  /// loading skin files and requesting available themes.
  /// </summary>
  /// <remarks>
  /// Skins may contain resources from different directories. All directory contents of a
  /// skin are added in a defined precedence. All the directory contents are added to the skin
  /// resource files.
  /// It is possible for a directory of a higher precedence to replace contents of directories
  /// of lower precedence.
  /// This class doesn't provide a sort of <i>reload</i> method, because to correctly
  /// reload a skin, we would have to check again all skin directories. This is not the
  /// job of this class, as it only manages skin directories which are given to it.
  /// To avoid heavy load times at startup, this class will only collect its resource files
  /// when requested (lazy initializing).
  /// </remarks>
  public class Skin: SkinResources
  {
    public const string SKIN_META_FILE = "skin.xml";
    public const string THEMES_DIRECTORY = "themes";
    public const string DEFAULT_THEME = "default";

    #region Protected fields

    /// <summary>
    /// Holds all known skin files, stored as a dictionary: The key is the theme name,
    /// the value is the <see cref="Theme"/> instance.
    /// </summary>
    protected IDictionary<string, Theme> _themes = null;

    // Meta information of the skin
    protected int _width;
    protected int _height;

    #endregion

    public Skin(string name): base(name, null)
    {
      // FIXME: read those parameters from skin metadata
      _width = 720;
      _height = 576;
    }

    public int Width
    {
      get { return _width; }
    }

    public int Height
    {
      get { return _height; }
    }

    public IDictionary<string, Theme> Themes
    {
      get
      {
        CheckResourcesInitialized();
        return _themes;
      }
    }

    public Theme DefaultTheme
    {
      get
      {
        CheckResourcesInitialized();
        if (_themes.ContainsKey(DEFAULT_THEME))
          return _themes[DEFAULT_THEME];
        IEnumerator<KeyValuePair<string, Theme>> enumer = _themes.GetEnumerator();
        if (enumer.MoveNext())
          return enumer.Current.Value;
        return null;
      }
    }

    /// <summary>
    /// Releases all lazy initialized resources. This will reduce the memory consumption
    /// of this instance.
    /// When requested again, the skin resources will be loaded again automatically.
    /// </summary>
    public override void Release()
    {
      base.Release();
      _themes = null;
    }

    /// <summary>
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected override void CheckResourcesInitialized()
    {
      if (_themes == null)
        _themes = new Dictionary<string, Theme>();
      base.CheckResourcesInitialized();
    }

    /// <summary>
    /// Adds the resources and themes in the specified directory.
    /// </summary>
    /// <param name="skinDirectory">Directory whose contents should be added
    /// to the file cache.</param>
    protected override void LoadDirectory(DirectoryInfo skinDirectory)
    {
      base.LoadDirectory(skinDirectory);
      ILogger logger = ServiceScope.Get<ILogger>();
      // Add themes
      foreach (DirectoryInfo themesDirectory in skinDirectory.GetDirectories(THEMES_DIRECTORY))
        // Iterate over 0 or 1 "themes" directories
        foreach (DirectoryInfo themeDirectory in themesDirectory.GetDirectories())
        { // Iterate over all themes subdirectories
          string themeName = themeDirectory.Name;
          Theme theme;
          if (_themes.ContainsKey(themeName))
            theme = _themes[themeName];
          else
            theme = _themes[themeName] = new Theme(themeName, this);
          theme.AddRootDirectory(themeDirectory);
        }
      // TODO: Load meta information file
    }
  }
}
