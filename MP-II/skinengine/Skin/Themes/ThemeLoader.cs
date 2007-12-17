#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Text;
using System.Xml;
using System.Drawing;
using Microsoft.DirectX.Direct3D;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
namespace SkinEngine.Skin
{
  public class ThemeLoader
  {
    public Theme Load(string themeName)
    {
      ServiceScope.Get<ILogger>().Debug("loading theme {0}", themeName);
      Theme theme = new Theme();
      Load(ref theme, "default");
      if (themeName != "default")
        Load(ref theme, themeName);
      theme.Name = themeName;
      return theme;
    }

    /// <summary>
    /// Loads the specified theme.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="themeName">Name of the theme.</param>
    void Load(ref Theme theme, string themeName)
    {
      XmlDocument doc = new XmlDocument();
      XmlTextReader reader = new XmlTextReader(String.Format(@"skin\{0}\themes\{1}\theme.xml", SkinContext.SkinName, themeName));
      doc.Load(reader);
      reader.Close();

      LoadColors(ref theme, doc);
      LoadImages(ref theme, doc);
      LoadFonts(ref theme, doc);
    }

    /// <summary>
    /// Loads the colors.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="doc">The doc.</param>
    void LoadColors(ref Theme theme, XmlDocument doc)
    {
      XmlNodeList nodes = doc.SelectNodes("/theme/colors/color");
      foreach (XmlNode node in nodes)
      {
        string name = GetName(node, "name");
        if (name != null && name.Length > 0)
        {
          theme.AddColor(name, GetColor(node));
        }
      }
    }
    /// <summary>
    /// Loads the images.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="doc">The doc.</param>
    void LoadImages(ref Theme theme, XmlDocument doc)
    {
      XmlNodeList nodes = doc.SelectNodes("/theme/images/image");
      foreach (XmlNode node in nodes)
      {
        string name = GetName(node, "name");
        if (name != null && name.Length > 0)
        {
          string image = GetName(node, "value");
          if (image != null && image.Length > 0)
          {
            theme.AddImage(name, image);
          }
        }
      }
    }
    /// <summary>
    /// Loads the fonts.
    /// </summary>
    /// <param name="theme">The theme.</param>
    /// <param name="doc">The doc.</param>
    void LoadFonts(ref Theme theme, XmlDocument doc)
    {
    }

    protected string GetName(XmlNode node, string attribute)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attribute);
      if (attrib == null)
      {
        return "";
      }
      string name = attrib.Value;
      if (name == null)
      {
        return "";
      }
      return name;
    }
    /// <summary>
    /// Gets the color from the xml node and returns the ColorValue
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>colorValue</returns>
    protected ColorValue GetColor(XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("value");
      if (attrib == null)
      {
        return ColorValue.FromColor(Color.White);
      }
      string colorString = attrib.Value;
      if (colorString == null)
      {
        return ColorValue.FromColor(Color.White);
      }
      string[] parts = colorString.Split(new char[] { ',' });
      if (parts.Length < 3)
      {
        return ColorValue.FromColor(Color.White);
      }
      float a, r, g, b;
      a = 255.0f;
      r = Convert.ToInt32(parts[0]);
      r /= 255.0f;
      g = Convert.ToInt32(parts[1]);
      g /= 255.0f;
      b = Convert.ToInt32(parts[2]);
      b /= 255.0f;
      if (parts.Length == 4)
      {
        a = Convert.ToInt32(parts[3]);
      }
      a /= 255.0f;
      if (a < 0)
      {
        a = 0;
      }
      if (a > 1)
      {
        a = 1.0f;
      }

      if (r < 0)
      {
        r = 0;
      }
      if (r > 1)
      {
        r = 1.0f;
      }

      if (g < 0)
      {
        g = 0;
      }
      if (g > 1)
      {
        g = 1.0f;
      }

      if (b < 0)
      {
        b = 0;
      }
      if (b > 1)
      {
        b = 1.0f;
      }
      return new ColorValue(r, g, b, a);
    }
  }
}
