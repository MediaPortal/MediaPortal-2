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
  public class VisualBrushBuilder : BuilderHelper, IControlBuilder
  {
    /// <summary>
    /// Creates the a new visual brush control and fills all its properties
    /// with the values specified in the xml node
    /// </summary>
    /// <param name="context">The skin loader context.</param>
    /// <param name="window">The window.</param>
    /// <param name="node">The xml node.</param>
    /// <param name="container">The container for the visual brush.</param>
    /// <param name="parent">The parent control of the visual brush.</param>
    /// <returns>new visualbrush control</returns>
    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                Control parent)
    {
      Context = context;
      VisualBrush brush = new VisualBrush(parent);
      brush.Width = GetFloat(node, "width", 0);
      brush.Height = GetFloat(node, "height", 0);
      //brush.PositionProperty = GetVector3(node, "position", container);
      Property position = GetPropertyVector3(node, brush, "position", new Vector3(0, 0, 0));
      if (parent != null)
      {
        brush.PositionProperty = new PositionDependency(parent.PositionProperty, position);
      }
      else
      {
        brush.PositionProperty = position;
      }
      brush.OriginalPosition = (Vector3)position.GetValue();
      brush.Color = GetColor(node);
      brush.Container = container;
      brush.Name = GetName(node);
      brush.Window = window;
      brush.Source = GetRectangle(node, "source");
      brush.Effect = GetName(node, "effect");
      brush.AlphaGradient = LoadAlphaGradient(node, window);
      brush.AlphaMask = GetVector4(node, "alphamask", new Vector4(1, 1, 1, 1));
      LoadClipPlane(window, node, brush, "clip");
      brush.IsVisibleProperty = GetPropertyBool(node, window, brush, "visible", true);
      brush.IsDepthTestEnabledProperty = GetBooleanCondition(node, brush, "depthtest", false);
      brush.TriggerProperty = GetPropertyBool(node, window, brush, "trigger", false);
      LoadAnimations(window, node, brush);

      List<Control> l = new List<Control>();
      l.Add(brush);
      return l;
    }
  }
}
