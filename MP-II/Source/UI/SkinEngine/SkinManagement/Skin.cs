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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Utilities;

namespace MediaPortal.SkinEngine.SkinManagement
{
  /// <summary>
  /// Represents a skin - which is a logical construct consisting of a logical name
  /// (skin name), some meta information (like a description, the native size and the preview image)
  /// and a set of skin files.
  /// This class provides methods for requesting the meta information, available skin files,
  /// loading skin files and requesting available themes.
  /// </summary>
  /// <remarks>
  /// The meta information will be read from a file <i>skin.xml</i> located in one of the
  /// skin resource directories.
  /// </remarks>
  public class Skin: SkinResources
  {
    public const string SKIN_META_FILE = "skin.xml";
    public const string THEMES_DIRECTORY = "themes";

    public const int MIN_SKIN_DESCRIPTOR_VERSION_HIGH = 1;
    public const int MIN_SKIN_DESCRIPTOR_VERSION_LOW = 0;

    #region Protected fields

    /// <summary>
    /// Holds all known skin files, stored as a dictionary: The key is the theme name,
    /// the value is the <see cref="Theme"/> instance.
    /// </summary>
    protected IDictionary<string, Theme> _themes = null;

    // Meta information of the skin
    protected bool _metadataInitialized = false;
    protected int _nativeWidth = -1;
    protected int _nativeHeight = -1;
    protected string _author = null;
    protected string _description = null;
    protected string _usageNote = null;
    protected string _previewResourceKey = null;
    protected string _specVersion = null;
    protected string _skinVersion = null;
    protected string _skinEngineVersion = null;
    protected string _defaultThemeName = null;

    #endregion

    public Skin(string name): base(name, null)
    {
    }

    public string ShortDescription
    {
      get { return _description; }
    }

    public string UsageNote
    {
      get { return _usageNote; }
    }

    public string PreviewResourceKey
    {
      get { return _previewResourceKey; }
    }

    /// <summary>
    /// Returns the information if the resources of this skin are complete
    /// (i.e. if the skin meta file could be read).
    /// </summary>
    public override bool IsValid
    {
      get
      {
        CheckMetadataInitialized();
        return _metadataInitialized;
      }
    }

    /// <summary>
    /// Returns the width, this skin was designed for. All sizes given in
    /// the skinfiles and its theme are based on this skin width. This value
    /// will be needed to calculate the width scale factor to render the screens in
    /// the whole screen width.
    /// </summary>
    public int NativeWidth
    {
      get
      {
        CheckMetadataInitialized();
        return _nativeWidth;
      }
    }

    /// <summary>
    /// Returns the height, this skin was designed for. All sizes given in
    /// the skinfiles and its theme are based on this skin height. This value
    /// will be needed to calculate the height scale factor to render the screens in
    /// the whole screen height.
    /// </summary>
    public int NativeHeight
    {
      get
      {
        CheckMetadataInitialized();
        return _nativeHeight;
      }
    }

    /// <summary>
    /// Returns all themes defined in this skin. Some of the returned themes may
    /// not be valid. To check if a theme is valid, use method <see cref="Theme.IsValid"/>.
    /// </summary>
    /// <value>The returned dictionary maps theme names to themes.</value>
    public IDictionary<string, Theme> Themes
    {
      get
      {
        CheckResourcesInitialized();
        return _themes;
      }
    }

    /// <summary>
    /// Returns the default theme of this skin.
    /// </summary>
    /// <value>Default theme of this skin, or the first theme, or <c>null</c> if no
    /// themes are defined.</value>
    public Theme DefaultTheme
    {
      get
      {
        CheckMetadataInitialized(); // Load default theme name
        CheckResourcesInitialized(); // Load themes
        if (_defaultThemeName != null && _themes.ContainsKey(_defaultThemeName))
          return _themes[_defaultThemeName];
        IEnumerator<KeyValuePair<string, Theme>> enumer = _themes.GetEnumerator();
        if (enumer.MoveNext())
          return enumer.Current.Value;
        return null;
      }
    }

    public override void Release()
    {
      base.Release();
      if (_themes != null)
        foreach (Theme theme in _themes.Values)
          theme.Release();
    }

    /// <summary>
    /// Will trigger the lazy metadata initialization on request.
    /// </summary>
    protected void CheckMetadataInitialized()
    {
      if (_metadataInitialized)
        return;
      string metaFilePath = GetResourceFilePath(SKIN_META_FILE);
      if (metaFilePath == null)
        return;
      _metadataInitialized = LoadMetadata(metaFilePath);
    }

    protected bool LoadMetadata(string metaFilePath)
    {
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.Load(metaFilePath);
        XmlElement skinElement = doc.DocumentElement;
        if (skinElement == null || skinElement.Name != "Skin")
          throw new ArgumentException("File is no skin descriptor (needs to contain a 'Skin' element)");

        bool versionOk = false;
        foreach (XmlAttribute attr in skinElement.Attributes)
        {
          switch (attr.Name)
          {
            case "Version":
              StringUtils.CheckVersionEG(attr.Value, MIN_SKIN_DESCRIPTOR_VERSION_HIGH, MIN_SKIN_DESCRIPTOR_VERSION_LOW);
              _specVersion = attr.Value;
              versionOk = true;
              break;
            case "Name":
              if (_name != null && _name != attr.Value)
                throw new ArgumentException("Skin name '" + _name + "' doesn't correspond to specified name '" + attr.Value + "'");
              else
                _name = attr.Value;
              break;
            default:
              throw new ArgumentException("Attribute '" + attr.Name + "' is unknown");
          }
        }
        if (!versionOk)
          throw new ArgumentException("Attribute 'Version' expected");

        foreach (XmlNode child in skinElement.ChildNodes)
        {
          switch (child.Name)
          {
            case "ShortDescription":
              _description = child.InnerText;
              break;
            case "UsageNote":
              _usageNote = child.InnerText;
              break;
            case "Preview":
              _previewResourceKey = child.InnerText;
              break;
            case "Author":
              _author = child.InnerText;
              break;
            case "SkinVersion":
              _skinVersion = child.InnerText;
              break;
            case "SkinEngine":
              _skinEngineVersion = child.InnerText;
              break;
            case "NativeWidth":
              _nativeWidth = Int32.Parse(child.InnerText);
              break;
            case "NativeHeight":
              _nativeHeight = Int32.Parse(child.InnerText);
              break;
            case "DefaultTheme":
              _defaultThemeName = child.InnerText;
              break;
            default:
              throw new ArgumentException("Child element '" + child.Name + "' is unknown");
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error parsing skin descriptor '" + metaFilePath + "'", e);
        return false;
      }
      return true;
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
    /// <param name="skinDirectoryPath">Path to a directory whose contents should be added
    /// to the file cache.</param>
    protected override void LoadDirectory(string skinDirectoryPath)
    {
      base.LoadDirectory(skinDirectoryPath);
      ILogger logger = ServiceScope.Get<ILogger>();
      // Add themes
      string themesDirectoryPath = Path.Combine(skinDirectoryPath, THEMES_DIRECTORY);
      if (Directory.Exists(themesDirectoryPath))
        foreach (string themeDirectoryPath in Directory.GetDirectories(themesDirectoryPath))
        { // Iterate over all themes subdirectories
          string themeName = Path.GetFileName(themeDirectoryPath);
          if (themeName.StartsWith("."))
            continue;
          Theme theme;
          if (_themes.ContainsKey(themeName))
            theme = _themes[themeName];
          else
            theme = _themes[themeName] = new Theme(themeName, this);
          theme.AddRootDirectory(themeDirectoryPath);
        }
    }
  }
}
