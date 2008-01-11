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
  public class ImageBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new image control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the image.</param>
    /// <param name="parent">The parent control of the image.</param>
    /// <returns>new image control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      string source;
      source = node.Attributes.GetNamedItem("source").Value;
      Image image = new Image(parent);
      image.Container = container;
      image.Color = GetColor(node);
      image.Align = GetAlignment(node);
      image.Name = GetName(node);
      image.Window = window;
      image.UseThumbNailProperty = GetPropertyBool(node, window, image, "usethumbnail", true);
      image.KeepAspectRatioProperty = GetPropertyBool(node, window, image, "keepaspectratio", false);
      image.ZoomProperty = GetPropertyBool(node, window, image, "zoom", false);
      image.Width = GetFloat(node, "width", 0);
      image.Height = GetFloat(node, "height", 0);
      //image.PositionProperty = GetVector3(node, "position", container);

      Property position = GetPropertyVector3(node, image, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        image.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        image.PositionProperty = position;
      }
      image.OriginalPosition = (Vector3)position.GetValue();
      image.AlphaGradient = LoadAlphaGradient(node, window);
      image.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      LoadClipPlane(window, node, image, "clip");

      image.ListItemProperty = GetPropertyBool(node, window, image, "item", null);
      image.IsVisibleProperty = GetPropertyBool(node, window, image, "visible", true);
      image.IsDepthTestEnabledProperty = GetBooleanCondition(node, image, "depthtest", false);
      image.EffectProperty = GetPropertyBool(node, window, image, "effect", "");
      image.SourceProperty = GetPropertyBool(node, window, image, "source", "");
      image.DefaultProperty = GetPropertyBool(node, window, image, "default", "");
      LoadAnimations(window, node, image);

      List<Control> l = new List<Control>();
      l.Add(image);
      return l;
    }
  }
}
