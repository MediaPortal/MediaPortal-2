#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Xml.XPath;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Fonts
{
  public class FontManager 
  {
    public const string DEFAULT_FONT_FILE = "default-font.xml";
    private static readonly IDictionary<string, FontFamily> _families = new Dictionary<string, FontFamily>();
    private static string _defaultFontFamily;
    private static int _defaultFontSize;

    /// <summary>
    /// Returns the default font family.
    /// </summary>
    public static string DefaultFontFamily
    {
      get { return _defaultFontFamily; }
    }

    /// <summary>
    /// Returns the default font size.
    /// </summary>
    public static int DefaultFontSize
    {
      get { return _defaultFontSize; }
    }

    /// <summary>
    /// Returns the Font object for the specified <paramref name="fontFamily"/> and
    /// <paramref name="fontSize"/>.
    /// </summary>
    public static Font GetScript(string fontFamily, int fontSize)
    {
      FontFamily family;
      if (!_families.TryGetValue(fontFamily, out family) || family == null)
        return null;

      return family.Addfont(fontSize, 96);
    }

    /// <summary>
    /// Sets the font manager up with the specified <paramref name="resourcesCollection"/>.
    /// This method will load the font defaults (family and size) and the font files from the
    /// resource collection.
    /// </summary>
    public static void Load(IResourceAccessor resourcesCollection)
    {
      Unload();
      string defaultFontFilePath = resourcesCollection.GetResourceFilePath(
          SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + DEFAULT_FONT_FILE);

      XPathDocument doc = new XPathDocument(defaultFontFilePath);

      XPathNavigator nav = doc.CreateNavigator();
      nav.MoveToChild(XPathNodeType.Element);
      _defaultFontFamily = nav.GetAttribute("FontFamily", string.Empty);
      string defaultFontSize = nav.GetAttribute("FontSize", string.Empty);
      _defaultFontSize = int.Parse(defaultFontSize);

      // Iterate over font family descriptors
      foreach (string descriptorFilePath in resourcesCollection.GetResourceFilePaths(
          "^" + SkinResources.FONTS_DIRECTORY + "\\\\.*\\.desc$").Values)
      {
        doc = new XPathDocument(descriptorFilePath);
        nav = doc.CreateNavigator();
        nav.MoveToChild(XPathNodeType.Element);
        string familyName = nav.GetAttribute("Name", string.Empty);
        if (string.IsNullOrEmpty(familyName))
          throw new ArgumentException("FontManager: Failed to parse family name for font descriptor file '{0}'", descriptorFilePath);
        string ttfFile = nav.GetAttribute("Ttf", string.Empty);
        if (string.IsNullOrEmpty(ttfFile))
          throw new ArgumentException("FontManager: Failed to parse ttf name for font descriptor file '{0}'", descriptorFilePath);

        string fontFilePath = resourcesCollection.GetResourceFilePath(
            SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + ttfFile);
        FontFamily family = new FontFamily(familyName, fontFilePath);
        _families[familyName] = family;
      }
    }

    public static void Unload()
    {
      foreach (KeyValuePair<string, FontFamily> fontFamily in _families)
        fontFamily.Value.Dispose();
      _families.Clear();
    }
  }
}
