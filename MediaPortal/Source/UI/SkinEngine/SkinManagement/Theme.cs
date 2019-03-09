#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Xml.XPath;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.SkinManagement
{
  /// <summary>
  /// Holds resource files for a theme. Typically, a theme consists of
  /// meta information (like theme name, author, ...) and some style resource files,
  /// but a theme may override any file from its skin or from the default theme or
  /// skin.
  /// </summary>
  /// <remarks>
  /// The meta information will be read from a file <i>theme.xml</i> located in one of the
  /// theme resource directories.
  /// </remarks>
  public class Theme: SkinResources, ITheme
  {
    public const string THEME_META_FILE = "theme.xml";

    public const int THEME_DESCRIPTOR_VERSION_MAJOR = 1;
    public const int MIN_THEME_DESCRIPTOR_VERSION_MINOR = 0;

    protected Skin _parentSkin;

    // Meta information of the theme
    protected bool _metadataInitialized = false;
    protected string _author = null;
    protected string _description = null;
    protected string _previewResourceKey = null;
    protected string _specVersion = null;
    protected string _themeVersion = null;
    protected string _skinEngineVersion = null;
    protected string _basedOnTheme = null;
    protected int _minColorDepth = -1;

    public Theme(string name, Skin parentSkin): base(name)
    {
      _parentSkin = parentSkin;
    }

    public string BasedOnTheme
    {
      get
      {
        CheckMetadataInitialized();
        return _basedOnTheme;
      }
    }

    public override string ShortDescription
    {
      get
      {
        CheckMetadataInitialized();
        return _description;
      }
    }

    public override string PreviewResourceKey
    {
      get
      {
        CheckMetadataInitialized();
        return _previewResourceKey;
      }
    }

    ISkin ITheme.ParentSkin
    {
      get { return _parentSkin; }
    }

    /// <summary>
    /// Returns the information if the resources of this skin are complete
    /// (i.e. if the theme meta file could be read).
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
    /// Returns the <see cref="Skin"/> this theme belongs to.
    /// </summary>
    public Skin ParentSkin
    {
      get { return _parentSkin; }
    }

    public override int SkinWidth
    {
      get { return _parentSkin.SkinWidth; }
    }

    public override int SkinHeight
    {
      get { return _parentSkin.SkinHeight; }
    }

    public override string SkinName
    {
      get { return _parentSkin.Name; }
    }

    internal override void SetupResourceChain(IDictionary<string, Skin> skins, Skin defaultSkin)
    {
      CheckMetadataInitialized();
      Theme inheritTheme;
      if (_basedOnTheme != null && _parentSkin.Themes.TryGetValue(_basedOnTheme, out inheritTheme))
        InheritedSkinResources = inheritTheme;
      else
      {
        SkinResources parentDefaultTheme = _parentSkin.DefaultTheme;
        InheritedSkinResources = parentDefaultTheme != null && parentDefaultTheme != this ? parentDefaultTheme : _parentSkin;
      }
      _inheritedSkinResources.SetupResourceChain(skins, defaultSkin);
    }

    /// <summary>
    /// Will trigger the lazy metadata initialization on request.
    /// </summary>
    protected void CheckMetadataInitialized()
    {
      if (_metadataInitialized)
        return;
      string metaFilePath = GetResourceFilePath(THEME_META_FILE);
      if (metaFilePath == null)
        return;
      _metadataInitialized = LoadMetadata(metaFilePath);
    }

    protected bool LoadMetadata(string metaFilePath)
    {
      try
      {
        XPathDocument doc = new XPathDocument(metaFilePath);
        XPathNavigator nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        if (nav.LocalName != "Theme")
          throw new ArgumentException("File is no theme descriptor (needs to contain a 'Theme' element)");

        bool versionOk = false;
        XPathNavigator attrNav = nav.Clone();
        if (attrNav.MoveToFirstAttribute())
          do
          {
            switch (attrNav.Name)
            {
              case "Version":
                Versions.CheckVersionCompatible(attrNav.Value, THEME_DESCRIPTOR_VERSION_MAJOR, MIN_THEME_DESCRIPTOR_VERSION_MINOR);
                _specVersion = attrNav.Value;
                versionOk = true;
                break;
              case "Name":
                if (_name != null && _name != attrNav.Value)
                  throw new ArgumentException("Theme name '" + _name + "' doesn't correspond to specified name '" + attrNav.Value + "'");
                _name = attrNav.Value;
                break;
              default:
                throw new ArgumentException("Attribute '" + attrNav.Name + "' is unknown");
            }
          } while (attrNav.MoveToNextAttribute());
        if (!versionOk)
          throw new ArgumentException("Attribute 'Version' expected");

        XPathNavigator childNav = nav.Clone();
        if (childNav.MoveToChild(XPathNodeType.Element))
          do
          {
            switch (childNav.LocalName)
            {
              case "ShortDescription":
                _description = childNav.Value;
                break;
              case "Preview":
                _previewResourceKey = childNav.Value;
                break;
              case "Author":
                _author = childNav.Value;
                break;
              case "ThemeVersion":
                _themeVersion = childNav.Value;
                break;
              case "SkinEngine":
                _skinEngineVersion = childNav.Value;
                break;
              case "MinColorDepth":
                _minColorDepth = Int32.Parse(childNav.Value);
                break;
              case "BasedOnTheme":
                _basedOnTheme = childNav.Value;
                break;
              default:
                throw new ArgumentException("Child element '" + childNav.Name + "' is unknown");
            }
          } while (childNav.MoveToNext(XPathNodeType.Element));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Error parsing theme descriptor '" + metaFilePath + "'", e);
        return false;
      }
      return true;
    }

    public override string ToString()
    {
      return string.Format("Theme '{0}' of skin '{1}'", _name, _parentSkin.Name);
    }
  }
}
