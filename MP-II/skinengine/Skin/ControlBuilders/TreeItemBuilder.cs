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

using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D9;
using SkinEngine.Controls;
using SkinEngine.Properties;

namespace SkinEngine.Skin
{
  public class TreeItemBuilder : BuilderHelper, IControlBuilder
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
      TreeItem treeitem = new TreeItem(parent);
      Control cont = GetContainer(window, node);
      if (cont != null)
      {
        treeitem.Container = cont;
      }
      else
      {
        treeitem.Container = container;
      }
      treeitem.Name = GetName(node);

      //button.PositionProperty = GetVector3(node, "position", (Control)button.Container);

      Property position = GetPropertyVector3(node, treeitem, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        treeitem.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        treeitem.PositionProperty = position;
      }
      treeitem.OriginalPosition = (Vector3)position.GetValue();
      treeitem.Width = GetFloat(node, "width", 0);
      treeitem.Height = GetFloat(node, "height", 0);

      treeitem.Window = window;
      treeitem.ListItemProperty = GetPropertyBool(node, window, treeitem, "item", null);
      treeitem.IsVisibleProperty = GetPropertyBool(node, window, treeitem, "visible", true);
      treeitem.MouseOnlyProperty = GetPropertyBool(node, window, treeitem, "mouseonly", false);
      treeitem.LabelProperty = GetPropertyBool(node, window, treeitem, "label", "");
      LoadCommand(window, node, treeitem);
      LoadContextMenu(window, node, treeitem);
      treeitem.IsFocusableProperty = GetPropertyBool(node, window, treeitem, "focusable", true);
      treeitem.CanFocusProperty = GetPropertyBool(node, window, treeitem, "canfocus", true);
      treeitem.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      treeitem.AlphaGradient = LoadAlphaGradient(node, window);
      treeitem.IsDepthTestEnabledProperty = GetBooleanCondition(node, treeitem, "depthtest", false);
      LoadClipPlane(window, node, treeitem, "clip");
      LoadCommand(window, node, treeitem);
      LoadAnimations(window, node, treeitem);
      string style = node.Attributes.GetNamedItem("style").Value;
      treeitem.Style = LoadStyle(window, treeitem, style, treeitem);

      List<Control> l = new List<Control>();
      l.Add(treeitem);
      return l;
    }
  }
}
