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
using SkinEngine.Fonts;
using SkinEngine.Properties;
using Font = SkinEngine.Fonts.Font;
namespace SkinEngine.Skin
{
  public class LabelBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new label control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the label.</param>
    /// <param name="parent">The parent control of the label.</param>
    /// <returns>new label control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      Label label = new Label(parent);
      string fontName = node.Attributes.GetNamedItem("font").Value;
      if (FontManager.Contains(fontName))
      {
        Font font = FontManager.GetScript(fontName);
        label.FontAsset = ContentManager.GetFont(font);
      }

      label.Width = GetFloat(node, "width", 0);
      label.Height = GetFloat(node, "height", 0);
      //label.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, label, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        label.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        label.PositionProperty = position;
      }
      label.OriginalPosition = (Vector3)position.GetValue();

      label.Color = GetColor(node);
      label.Container = container;
      label.Align = GetAlignment(node);
      label.Name = GetName(node);
      label.ScrollProperty = GetPropertyBool(node, window, label, "scroll", false);
      label.Window = window;
      label.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      label.AlphaGradient = LoadAlphaGradient(node, window);
      LoadClipPlane(window, node, label, "clip");

      Control cont = GetContainer(window, node);
      if (cont != null)
      {
        label.Container = cont;
      }
      label.ListItemProperty = GetPropertyBool(node, window, label, "item", null);
      label.IsVisibleProperty = GetPropertyBool(node, window, label, "visible", true);
      label.IsDepthTestEnabledProperty = GetBooleanCondition(node, label, "depthtest", false);
      Property labelProperty = GetPropertyBool(node, window, label, "label", null);
      if (labelProperty != null)
      {
        label.LabelProperty = labelProperty;
      }
      LoadAnimations(window, node, label);
      List<Control> l = new List<Control>();
      l.Add(label);
      return l;
    }
  }
}
