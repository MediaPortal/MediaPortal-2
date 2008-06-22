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

namespace Presentation.SkinEngine.SkinManagement
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

    /// <summary>
    /// Returns the information if the resources of this skin are complete
    /// (i.e. if the skin meta file is present).
    /// </summary>
    public override bool IsValid
    {
      get
      {
        CheckMetadataInitialized();
        return _metadataInitialized;
      }
    }

    public int NativeWidth
    {
      get
      {
        CheckMetadataInitialized();
        return _nativeWidth;
      }
    }

    public int NativeHeight
    {
      get
      {
        CheckMetadataInitialized();
        return _nativeHeight;
      }
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
        if (_defaultThemeName != null && _themes.ContainsKey(_defaultThemeName))
          return _themes[_defaultThemeName];
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
    /// Will trigger the lazy metadata initialization on request.
    /// </summary>
    protected void CheckMetadataInitialized()
    {
      if (_metadataInitialized)
        return;
      FileInfo metaFile = GetResourceFile(SKIN_META_FILE);
      _metadataInitialized = LoadMetadata(metaFile);
    }

    protected bool LoadMetadata(FileInfo metaFile)
    {
      try
      {
        XmlDocument doc = new XmlDocument();
        using (FileStream fs = metaFile.OpenRead())
          doc.Load(fs);
        XmlElement themeElement = doc.DocumentElement;

        bool versionOk = false;
        foreach (XmlAttribute attr in themeElement.Attributes)
        {
          switch (attr.Name)
          {
            case "Version":
              CheckVersion(attr.Value, MIN_SKIN_DESCRIPTOR_VERSION_HIGH, MIN_SKIN_DESCRIPTOR_VERSION_LOW);
              _specVersion = attr.Value;
              versionOk = true;
              break;
            case "Name":
              if (_name != null && _name != attr.Value)
                throw new ArgumentException("Theme name '" + _name + "' doesn't correspond to specified name '" + attr.Value + "'");
              else
                _name = attr.Value;
              break;
            default:
              throw new ArgumentException("Attribute '" + attr.Name + "' is unknown");
          }
        }
        if (!versionOk)
          throw new ArgumentException("Attribute 'Version' expected");

        foreach (XmlNode child in themeElement.ChildNodes)
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
              throw new ArgumentException("Error parsing skin descriptor: child element '" + child.Name + "' is unknown");
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error parsing skin descriptor: " + e.Message, e);
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
