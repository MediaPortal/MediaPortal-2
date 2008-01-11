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
using SkinEngine.Controls;
using MediaPortal.Core;

namespace SkinEngine.Skin
{
  public class RepeaterBuilder : BuilderHelper, IControlBuilder
  {
    #region IControlBuilder Members

    public List<Control> Create(SkinLoaderContext context, Window window, XmlNode node, Control container, Control parent)
    {
      Context = context;
      List<Control> controls = new List<Control>();

      int start = (int)GetFloatProperty(node, "start", 0).Evaluate(container, container);
      int end = (int)GetFloatProperty(node, "end", 0).Evaluate(container, container);
      for (int i = start; i < end; ++i)
      {
        Context.Index = i;
        ISkinLoader loader = ServiceScope.Get<ISkinLoader>();
        foreach (XmlNode nodeChild in node.ChildNodes)
        {
          List<Control> subCons = loader.CreateControl(Context, window, nodeChild, container, container);
          if (subCons != null)
          {
            foreach (Control c in subCons)
            {
              if (c.Name.Length > 0)
              {
                window.AddNamedControl(c);
              }
              //LoadAnimations(window,node.SelectSingleNode("controls"), control);
              controls.Add(c);
            }
          }
        }
      }
      return controls;
    }

    #endregion
  }
}
