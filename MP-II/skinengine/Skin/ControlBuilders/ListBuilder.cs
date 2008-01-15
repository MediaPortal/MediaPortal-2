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
using MediaPortal.Core.Commands;
using SlimDX;
using SlimDX.Direct3D9;
using SkinEngine.Controls;
using SkinEngine.Properties;

namespace SkinEngine.Skin
{
  public class ListBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new ListContainer control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the ListContainer.</param>
    /// <param name="parent">The parent control of the ListContainer.</param>
    /// <returns>new ListContainer control including all its styles</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      string items = GetName(node, "items");
      ListContainer list = new ListContainer(parent, items);
      //list.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, list, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        list.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        list.PositionProperty = position;
      }
      list.OriginalPosition = (Vector3)position.GetValue();
      string styles = node.Attributes.GetNamedItem("style").Value;
      list.Styles = LoadStyles(window, list, styles, list);
      list.Container = container;
      list.Color = GetColor(node);
      list.Name = GetName(node);
      list.Window = window;
      list.Styles.SelectedStyleIndex = 0;
      list.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      list.AlphaGradient = LoadAlphaGradient(node, window);
      LoadCommand(window, node, list);
      LoadContextMenu(window, node, list);
      LoadClipPlane(window, node, list, "clip");
      list.IsVisibleProperty = GetPropertyBool(node, window, list, "visible", true);
      list.IsDepthTestEnabledProperty = GetBooleanCondition(node,  list, "depthtest", false);
      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window,node, list,"onselecteditemchanged","onselecteditemchangedparameter","onselecteditemchangedresult", out cmd, out param, out cmdResult);
      list.OnSelectedItemChangeCommand = cmd;
      LoadAnimations(window, node, list);
      List<Control> l = new List<Control>();
      l.Add(list);
      return l;
    }
  }
}
