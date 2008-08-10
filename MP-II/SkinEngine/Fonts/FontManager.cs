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
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Fonts
{


  public class FontManager
  {
    public const string FONT_META_FILE = "fonts.xml";
    private static Dictionary<string, FontFamily> _families;
    private static string _defaultFontFamily;
    private static int _defaultFontSize;

    /// <summary>
    /// Parses the font file name for a font family
    /// </summary>
    private static string ParseTtfFile(XmlNode node)
    {
      try
      {
        XmlNode nodeName = node.Attributes.GetNamedItem("ttf");
        if (nodeName == null || nodeName.Value == null)
        {
          return String.Empty;
        }
        return nodeName.Value;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("FontManager failed to Parse ttf name.");
        ServiceScope.Get<ILogger>().Error(ex);
        return String.Empty;
      }
    }
    /// <summary>
    /// Parses a family name for a font family
    /// </summary>
    /// 
    private static string ParseFamilyName(XmlNode node)
    {
      try
      {
        XmlNode nodeFamilyName = node.Attributes.GetNamedItem("name");
        if (nodeFamilyName == null || nodeFamilyName.Value == null)
        {
          return String.Empty;
        }
        return nodeFamilyName.Value;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("FontManager failed to Parse family name.");
        ServiceScope.Get<ILogger>().Error(ex);
        return String.Empty;
      }
    }
    /// <summary>
    /// Returns the default font family
    /// </summary>
    public static string DefaultFontFamily
    {
      get { return _defaultFontFamily; }
    }

    /// <summary>
    /// Returns the default font size
    /// </summary>
    public static int DefaultFontSize
    {
      get { return _defaultFontSize; }
    }

    /// <summary>
    /// Returns the Font object
    /// </summary>
    /// 
    public static Font GetScript(string fontFamily, int fontSize)
    {

      FontFamily family = _families[fontFamily];

      if (family == null)
        return null;

      // Do we already have this font?
      foreach (Font font in family.FontList)
      {
        if (font.Size == fontSize)
        {
          return font;
        }
      }
      // No - create it.
      return family.Addfont(fontSize, 96);
    }

    /// <summary>
    /// Parses the fonts.xml file and loads the font familys it contains.
    /// </summary>
    /// 
    public static void Load()
    {
      _families = new Dictionary<string, FontFamily>();

      FileInfo descrFile = SkinContext.SkinResources.GetResourceFile(  
      SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + FONT_META_FILE);

      XmlDocument doc = new XmlDocument();
      doc.Load(descrFile.FullName);

      _defaultFontFamily = doc.DocumentElement.GetAttribute("defaultFamily");
      string defaultFontSize = doc.DocumentElement.GetAttribute("defaultSize");
      _defaultFontSize = int.Parse(defaultFontSize);

      XmlNodeList nodesItems = doc.SelectNodes("/Skin/family");
      foreach (XmlNode nodeItem in nodesItems)
      {
        string familyName = ParseFamilyName(nodeItem);
        string ttfFile = ParseTtfFile(nodeItem);
        FileInfo fontFile =
          SkinContext.SkinResources.GetResourceFile(
              SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + ttfFile);
        FontFamily family = new FontFamily(familyName, fontFile.FullName);
        _families[familyName] = family;
      }
    }
  }
}
