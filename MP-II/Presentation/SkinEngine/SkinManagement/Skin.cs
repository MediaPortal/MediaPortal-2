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
using MediaPortal.Utilities.Files;
using Presentation.SkinEngine.Loader;

namespace Presentation.SkinEngine
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
  public class Skin
  {
    public const string SKIN_META_FILE = "skin.xml";
    public const string THEMES_DIRECTORY = "themes";
    public const string THEME_META_FILE = THEMES_DIRECTORY + "\\theme.xml";
    public const string SCREENFILES_DIRECTORY = "screenfiles";
    public const string DEFAULT_THEME = "default";
    public const string FONTS_DIRECTORY = "fonts";
    public const string FONT_META_FILE = FONTS_DIRECTORY + "\\fonts.xml";
    public const string SHADERS_DIRECTORY = "shaders";
    public const string MEDIA_DIRECTORY = "media";

    #region Variables

    /// <summary>
    /// Holds a list of directories which will be loaded when the skin gets lazy initialized.
    /// </summary>
    protected IList<DirectoryInfo> _skinDirectories = new List<DirectoryInfo>();

    /// <summary>
    /// Holds all known resource files in this skin, stored as a dictionary: The key is the unified
    /// resource file name (relative path name starting at the beginning of the skinfile directory),
    /// the value is the <see cref="FileInfo"/> instance of the resource file.
    /// This instance will be lazy initialized.
    /// </summary>
    protected IDictionary<string, FileInfo> _resourceFiles = null;

    /// <summary>
    /// Holds all known skin files, stored as a dictionary: The key is the theme name,
    /// the value is the <see cref="Theme"/> instance.
    /// </summary>
    protected IDictionary<string, Theme> _themes = null;

    // Meta information of the skin
    protected string _name;

    protected int _width;
    protected int _height;

    #endregion

    public Skin(string name)
    {
      _name = name;
      // FIXME: read those parameters from skin metadata
      _width = 720;
      _height = 576;
    }

    public string Name
    {
      get { return _name; }
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
        CheckInitialized();
        return _themes;
      }
    }

    public Theme DefaultTheme
    {
      get
      {
        CheckInitialized();
        return _themes[DEFAULT_THEME]; 
      }
    }

    /// <summary>
    /// Adds a directory to the internal skinfile resources.
    /// This method will add all relevant skin files in the specified directory to the
    /// skin file cache.
    /// </summary>
    /// <param name="skinDirectory">Directory which is the root of a directory tree
    /// for this skin.</param>
    public void AddSkinDirectory(DirectoryInfo skinDirectory)
    {
      _skinDirectories.Add(skinDirectory);
    }

    /// <summary>
    /// Returns the resource file for the specified resource name.
    /// </summary>
    /// <param name="resourceName">Name of the resource. This is the
    /// path of the resource relative to the skin directory.</param>
    /// <returns></returns>
    public FileInfo GetResourceFile(string resourceName)
    {
      CheckInitialized();
      if (_resourceFiles.ContainsKey(resourceName))
        return _resourceFiles[resourceName];
      else
        return null;
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
    /// Will trigger the lazy initialization on request.
    /// </summary>
    protected void CheckInitialized()
    {
      if (_resourceFiles == null || _themes == null)
      {
        _resourceFiles = new Dictionary<string, FileInfo>();
        _themes = new Dictionary<string, Theme>();
        foreach (DirectoryInfo skinDirectory in _skinDirectories)
          LoadDirectory(skinDirectory);
      }
    }

    /// <summary>
    /// Adds the specified directory to the cache of resource files.
    /// Also adds the themes in the specified directory.
    /// </summary>
    /// <param name="skinDirectory">Directory to add to the file cache.</param>
    protected void LoadDirectory(DirectoryInfo skinDirectory)
    {
      ILogger logger = ServiceScope.Get<ILogger>();
      // Add resource files for this directory
      int directoryNameLength = skinDirectory.FullName.Length;
      foreach (FileInfo resourceFile in FileUtils.GetAllFilesRecursively(skinDirectory))
      {
        string resourceName = resourceFile.FullName;
        resourceName = resourceName.Substring(directoryNameLength);
        if (resourceName.StartsWith(Path.DirectorySeparatorChar.ToString()))
          resourceName = resourceName.Substring(1);
        if (_resourceFiles.ContainsKey(resourceName))
          logger.Info("Duplicate resource file for skin '{0}': '{1}', '{2}'", _name, _resourceFiles[resourceName].FullName, resourceName);
        else
          _resourceFiles[resourceName] = resourceFile;
      }
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
          theme.AddThemeDirectory(themeDirectory);
        }
    }
  }
}
