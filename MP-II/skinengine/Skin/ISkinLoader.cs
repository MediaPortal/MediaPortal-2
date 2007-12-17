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

using System.Collections.Generic;
using System.Xml;
using SkinEngine.Controls;

namespace SkinEngine.Skin
{
  public interface ISkinLoader
  {
    /// <summary>
    /// Loads the specified window.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="windowName">Name of the window.</param>
    void Load(Window window, string windowName);

    /// <summary>
    /// Registers a new control builder.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="builder">The builder.</param>
    void RegisterControlBuilder(string name, IControlBuilder builder);

    /// <summary>
    /// Creates a control.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="container">The container.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    List<Control> CreateControl(SkinLoaderContext context, Window window, XmlNode node, Control container, Control parent);

    Dictionary<string, IControlBuilder> Builders { get; }
    Style LoadStyle(Window window, Control container, string styleName, Control parent);
    StylesCollection LoadStyles(Window window, Control container, string styleName, Control parent);
  }
}