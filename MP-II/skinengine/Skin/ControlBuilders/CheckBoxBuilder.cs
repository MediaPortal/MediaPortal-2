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
using Microsoft.DirectX;
using SkinEngine.Controls;
using SkinEngine.Properties;
using MediaPortal.Core.Commands;

namespace SkinEngine.Skin
{
  public class CheckBoxBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new checkbox control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the checkbox.</param>
    /// <param name="parent">The parent control of the checkbox.</param>
    /// <returns>new checkbox control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      CheckBox checkbox = new CheckBox(parent);
      //checkbox.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, checkbox, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        checkbox.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        checkbox.PositionProperty = position;
      }
      checkbox.OriginalPosition = (Vector3)position.GetValue();


      checkbox.Window = window;
      checkbox.Container = container;
      checkbox.Name = GetName(node);
      LoadCommand(window, node, checkbox);
      LoadContextMenu(window, node, checkbox);
      checkbox.LabelProperty = GetPropertyBool(node, window, checkbox, "label", "");
      checkbox.IsVisibleProperty = GetPropertyBool(node, window, checkbox, "visible", true);
      checkbox.IsDepthTestEnabledProperty = GetBooleanCondition(node,  checkbox, "depthtest", false);
      checkbox.ListItemProperty = GetPropertyBool(node, window, checkbox, "item", null);
      checkbox.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      checkbox.AlphaGradient = LoadAlphaGradient(node, window);
      checkbox.IsSelectedProperty = GetPropertyBool(node, window, checkbox, "selected", false);
      string style = node.Attributes.GetNamedItem("style").Value;
      checkbox.Style = LoadStyle(window, checkbox, style, checkbox);
      LoadAnimations(window, node, checkbox);
      LoadClipPlane(window, node, checkbox, "clip");

      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window, node, checkbox, "onselecteditemchanged", "onselecteditemchangedparameter", "onselecteditemchangedresult", out cmd, out param, out cmdResult);
      checkbox.OnSelectedItemChangeCommand = cmd;

      List<Control> l = new List<Control>();
      l.Add(checkbox);
      return l;
    }
  }
}
