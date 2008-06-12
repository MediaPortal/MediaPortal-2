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
using System.Diagnostics;
using System.IO;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Fonts
{
  public class FontManager
  {
    private static Dictionary<string, Font> _fonts;

    private static void LoadFont(XmlNode node)
    {
      try
      {
        XmlNode nodeName = node.Attributes.GetNamedItem("name");
        if (nodeName == null)
        {
          return;
        }
        if (nodeName.Value == null)
        {
          return;
        }
        string fontName = nodeName.Value;
        if (fontName.Length == 0)
        {
          return;
        }
        ServiceScope.Get<ILogger>().Debug("FontManager: LoadFont: {0}", fontName);

        XmlNode nodeFace = node.Attributes.GetNamedItem("face");
        if (nodeFace == null)
        {
          return;
        }
        if (nodeFace.Value == null)
        {
          return;
        }
        string fontFace = nodeFace.Value;
        if (fontFace.Length == 0)
        {
          return;
        }

        XmlNode nodeSize = node.Attributes.GetNamedItem("size");
        if (nodeSize == null)
        {
          return;
        }
        if (nodeSize.Value == null)
        {
          return;
        }
        string fontSize = nodeSize.Value;
        if (fontSize.Length == 0)
        {
          return;
        }
        int size;
        Int32.TryParse(fontSize, out size);

        Font font = new Font(fontFace + ".fnt", size);
        font.OnCreateDevice(GraphicsDevice.Device);
        font.OnResetDevice(GraphicsDevice.Device);
        _fonts[fontName] = font;
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("FontManager failed to LoadFont");
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    public static bool Contains(string fontName)
    {
      return _fonts.ContainsKey(fontName);
    }

    public static Font GetScript(string fontName)
    {
      return _fonts[fontName];
    }

    public static void Free()
    {
      Dictionary<string, Font>.Enumerator enumer = _fonts.GetEnumerator();
      while (enumer.MoveNext())
      {
        Font f = enumer.Current.Value;
        Trace.WriteLine("dispose font" + enumer.Current.Key);
        f.OnLostDevice();
        f.OnDestroyDevice();
      }
    }

    public static void Alloc()
    {
      Dictionary<string, Font>.Enumerator enumer = _fonts.GetEnumerator();
      while (enumer.MoveNext())
      {
        Font f = enumer.Current.Value;
        //f.Reload();
        f.OnCreateDevice(GraphicsDevice.Device);
        f.OnResetDevice(GraphicsDevice.Device);
        Trace.WriteLine("alloc font:" + enumer.Current.Key);
      }
    }

    public static void Reload()
    {
      _fonts = new Dictionary<string, Font>();
      IDictionary<string, FileInfo> fontResources =
          SkinContext.SkinResources.GetResourceFiles(
              SkinResources.FONTS_DIRECTORY + "\\" + Path.DirectorySeparatorChar + ".*\\.xml");
      foreach (KeyValuePair<string, FileInfo> kvp in fontResources)
      {
        FileInfo fontFile = kvp.Value;
        XmlDocument doc = new XmlDocument();
        doc.Load(fontFile.FullName);
        LoadFont(doc.DocumentElement);
      }
    }
  }
}
