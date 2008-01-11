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
using System.Text;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Skin
{
  public class Theme
  {
    string _name;
    Dictionary<string, ColorValue> _colors;
    Dictionary<string, string> _images;

    /// <summary>
    /// Initializes a new instance of the <see cref="Theme"/> class.
    /// </summary>
    public Theme()
    {
      _colors = new Dictionary<string, ColorValue>();
      _images = new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets or sets the theme name.
    /// </summary>
    /// <value>The theme name.</value>
    public string Name
    {
      get
      {
        return _name;
      }
      set
      {
        _name = value;
      }
    }
    /// <summary>
    /// Determines whether the theme has defined the color.
    /// </summary>
    /// <param name="name">The colorname.</param>
    /// <returns>
    /// 	<c>true</c> if theme contains the color; otherwise, <c>false</c>.
    /// </returns>
    public bool HasColor(string name)
    {
      return _colors.ContainsKey(name);
    }

    /// <summary>
    /// Gets the color.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public ColorValue GetColor(string name)
    {
      if (!_colors.ContainsKey(name)) return new ColorValue(255, 0, 0, 255);
      return _colors[name];
    }

    /// <summary>
    /// Adds the color.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="color">The color.</param>
    public void AddColor(string name, ColorValue color)
    {
      _colors[name] = color;
    }


    /// <summary>
    /// Gets the Image.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public string GetImage(string name)
    {
      if (!_images.ContainsKey(name)) return "";
      return _images[name];
    }

    public bool HasImage(string name)
    {
      return _images.ContainsKey(name);
    }
    public void AddImage(string name, string Image)
    {
      _images[name] = Image;
    }
  }
}
