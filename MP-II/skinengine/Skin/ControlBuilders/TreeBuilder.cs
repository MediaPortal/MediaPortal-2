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
  public class TreeBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new TreeContainer control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the TreeContainer.</param>
    /// <param name="parent">The parent control of the TreeContainer.</param>
    /// <returns>new TreeContainer control including all its styles</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container, Control parent)
    {
      Context = context;
      string items = GetName(node, "items");
      TreeContainer tree = new TreeContainer(parent, items);
      //list.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, tree, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        tree.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        tree.PositionProperty = position;
      }
      tree.OriginalPosition = (Vector3)position.GetValue();
      tree.Width = GetFloat(node, "width", 0);
      tree.Height = GetFloat(node, "height", 0);
      string styles = node.Attributes.GetNamedItem("style").Value;
      tree.Styles = LoadStyles(window, tree, styles, tree);
      tree.Container = container;
      tree.Color = GetColor(node);
      tree.Name = GetName(node);
      tree.Window = window;
      tree.Styles.SelectedStyleIndex = 0;
      tree.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      tree.AlphaGradient = LoadAlphaGradient(node, window);
      LoadCommand(window, node, tree);
      LoadContextMenu(window, node, tree);
      LoadClipPlane(window, node, tree, "clip");
      tree.IsVisibleProperty = GetPropertyBool(node, window, tree, "visible", true);
      tree.IsDepthTestEnabledProperty = GetBooleanCondition(node, tree, "depthtest", false);
      //ICommand cmd;
      //ICommandParameter param;
      //ICommandResult cmdResult;
      //GetCommand(window, node, list, "onselecteditemchanged", "onselecteditemchangedparameter", "onselecteditemchangedresult", out cmd, out param, out cmdResult);
      //list.OnSelectedItemChangeCommand = cmd;
      LoadAnimations(window, node, tree);
      List<Control> l = new List<Control>();
      l.Add(tree);
      return l;
    }
  }
}
