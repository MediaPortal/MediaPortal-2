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
  public class KeyboardBuilder : BuilderHelper, IControlBuilder
  {
    //public Control Create(Window window, System.Xml.XmlNode node, Control container, Control parent)
    //{
    //    Keyboard keyb = new Keyboard(parent);
    //    Control cont = GetContainer(window, node);
    //    if (cont != null)
    //        keyb.Container = cont;
    //    else
    //        keyb.Container = container;
    //    keyb.PositionProperty = GetVector3(node, "position", (Control)keyb.Container);

    //    string styles = node.Attributes.GetNamedItem("style").Value;
    //    keyb.Styles = LoadStyles(window, keyb, styles, keyb);
    //    keyb.Name = GetName(node);
    //    keyb.Command = GetCommand(node);
    //    keyb.IsFocusable = GetBoolean(node, "focusable", true);
    //    keyb.CanFocus = GetBoolean(node, "canfocus", true);
    //    keyb.Window = window;
    //    keyb.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
    //    keyb.AlphaGradient = LoadAlphaGradient(node);
    //    keyb.IsVisibleProperty = GetBooleanCondition(node, keyb, "visible", true);
    //    keyb.ListItemProperty = GetItemProperty(node);
    //    LoadClipPlane(window, node, keyb, "clip");
    //    LoadCommand(window, node, keyb);
    //    LoadAnimations(window, node, keyb);
    //    return keyb;
    //}
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      Keyboard keyb = new Keyboard(parent);
      //keyb.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, keyb, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        keyb.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        keyb.PositionProperty = position;
      }
      keyb.OriginalPosition = (Vector3)position.GetValue();

      string styles = node.Attributes.GetNamedItem("style").Value;
      keyb.Styles = LoadStyles(window, keyb, styles, keyb);
      keyb.Container = container;
      keyb.Color = GetColor(node);
      keyb.Name = GetName(node);
      keyb.Window = window;
      keyb.Styles.SelectedStyleIndex = 0;
      keyb.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      keyb.AlphaGradient = LoadAlphaGradient(node, window);
      LoadCommand(window, node, keyb);
      LoadClipPlane(window, node, keyb, "clip");
      keyb.IsVisibleProperty = GetPropertyBool(node, window, keyb, "visible", true);
      keyb.IsDepthTestEnabledProperty = GetBooleanCondition(node, keyb, "depthtest", false);
      LoadAnimations(window, node, keyb);
      List<Control> l = new List<Control>();
      l.Add(keyb);
      return l;
    }
  }
}
