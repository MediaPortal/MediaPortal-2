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

namespace SkinEngine.Skin
{
  public class UpDownBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new UpDown control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the UpDown.</param>
    /// <param name="parent">The parent control of the UpDown.</param>
    /// <returns>new UpDown control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      UpDown upDown = new UpDown(parent);
      //upDown.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, upDown, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        upDown.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        upDown.PositionProperty = position;
      }
      upDown.OriginalPosition = (Vector3)position.GetValue();
      upDown.Container = (ListContainer) GetContainer(window, node);
      upDown.Name = GetName(node);
      LoadCommand(window, node, upDown);
      LoadContextMenu(window, node, upDown);
      upDown.Window = window;
      upDown.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      upDown.AlphaGradient = LoadAlphaGradient(node, window);
      upDown.IsVisibleProperty = GetPropertyBool(node, window, upDown, "visible", true);
      upDown.LabelProperty = GetPropertyBool(node, window, upDown, "label", "");
      upDown.IsDepthTestEnabledProperty = GetBooleanCondition(node, upDown, "depthtest", false);
      string style = node.Attributes.GetNamedItem("style").Value;
      upDown.Style = LoadStyle(window, upDown, style, upDown);
      LoadAnimations(window, node, upDown);
      LoadClipPlane(window, node, upDown, "clip");
      List<Control> l = new List<Control>();
      l.Add(upDown);
      return l;
    }
  }
}
