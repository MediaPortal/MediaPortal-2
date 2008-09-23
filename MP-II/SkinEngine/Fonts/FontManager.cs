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
using MediaPortal.Presentation.Screen;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Fonts
{


  public class FontManager 
  {
    public const string DEFAULT_FONT_FILE = "default-font.xml";
    private static IDictionary<string, FontFamily> _families;
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

      return family.Addfont(fontSize, 96);
    }

    /// <summary>
    /// Sets the font manager up with the specified <paramref name="resourcesCollection"/>.
    /// This method will load the font defaults (family and size) and the font files from the
    /// resource collection.
    /// </summary>
    public static void Load(IResourceAccessor resourcesCollection)
    {
      FileInfo defaultFontFile = resourcesCollection.GetResourceFile(  
          SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + DEFAULT_FONT_FILE);

      XmlDocument doc = new XmlDocument();
      using (FileStream stream = defaultFontFile.OpenRead())
        doc.Load(stream);

      _defaultFontFamily = doc.DocumentElement.GetAttribute("FontFamily");
      string defaultFontSize = doc.DocumentElement.GetAttribute("FontSize");
      _defaultFontSize = int.Parse(defaultFontSize);

      _families = new Dictionary<string, FontFamily>();
      // Iterate over font family descriptors
      foreach (FileInfo descriptorFile in resourcesCollection.GetResourceFiles(SkinResources.FONTS_DIRECTORY + "\\\\.*.desc").Values)
      {
        using (FileStream stream = descriptorFile.OpenRead())
          doc.Load(stream);
        string familyName = ParseFamilyName(doc.DocumentElement);
        string ttfFile = ParseTtfFile(doc.DocumentElement);
        FileInfo fontFile = resourcesCollection.GetResourceFile(
                SkinResources.FONTS_DIRECTORY + Path.DirectorySeparatorChar + ttfFile);
        FontFamily family = new FontFamily(familyName, fontFile.FullName);
        _families[familyName] = family;
      }
    }

    public static void Unload()
    {
      foreach (KeyValuePair<string, FontFamily> fontFamily in _families)
      {
        fontFamily.Value.Dispose();
      }
    }
  }
}
