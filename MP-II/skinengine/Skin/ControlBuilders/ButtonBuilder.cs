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
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using SkinEngine.Controls;
using SkinEngine.Properties;

namespace SkinEngine.Skin
{
  public class ButtonBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new button control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the button.</param>
    /// <param name="parent">The parent control of the button.</param>
    /// <returns>new button control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      Button button = new Button(parent);
      Control cont = GetContainer(window, node);
      if (cont != null)
      {
        button.Container = cont;
      }
      else
      {
        button.Container = container;
      }
      button.Name = GetName(node);

      //button.PositionProperty = GetVector3(node, "position", (Control)button.Container);

      Property position = GetPropertyVector3(node, button, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        button.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        button.PositionProperty = position;
      }
      button.OriginalPosition = button.Position;
      button.Width = GetFloat(node, "width", 0);
      button.Height = GetFloat(node, "height", 0);

      button.Window = window;
      button.ListItemProperty = GetPropertyBool(node, window, button, "item", null);
      button.IsVisibleProperty = GetPropertyBool(node, window, button, "visible", true);
      button.MouseOnlyProperty = GetPropertyBool(node, window, button, "mouseonly", false);
      button.LabelProperty = GetPropertyBool(node, window, button, "label", "");
      LoadCommand(window, node, button);
      LoadContextMenu(window, node, button);
      button.IsFocusableProperty = GetPropertyBool(node, window, button, "focusable", true);
      button.CanFocusProperty = GetPropertyBool(node, window, button, "canfocus", true);
      button.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      button.AlphaGradient = LoadAlphaGradient(node, window);
      button.IsDepthTestEnabledProperty = GetBooleanCondition(node,  button, "depthtest", false);
      LoadClipPlane(window, node, button, "clip");
      LoadCommand(window, node, button);
      LoadAnimations(window, node, button);
      string style = node.Attributes.GetNamedItem("style").Value;
      button.Style = LoadStyle(window,  button, style, button);

      List<Control> l = new List<Control>();
      l.Add(button);
      return l;
    }
  }
}