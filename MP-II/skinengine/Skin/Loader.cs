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

using System;
using System.Collections.Generic;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SkinEngine.Controls;

namespace SkinEngine.Skin
{
  public class Loader : BuilderHelper, ISkinLoader
  {
    #region variables

    private Dictionary<string, IControlBuilder> _listBuilders;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Loader"/> class.
    /// </summary>
    public Loader()
    {
      _listBuilders = new Dictionary<string, IControlBuilder>();
      RegisterControlBuilder("button", new ButtonBuilder());
      RegisterControlBuilder("checkbox", new CheckBoxBuilder());
      RegisterControlBuilder("group", new GroupBuilder());
      RegisterControlBuilder("image", new ImageBuilder());
      RegisterControlBuilder("keybinding", new KeyBindingBuilder());
      RegisterControlBuilder("label", new LabelBuilder());
      RegisterControlBuilder("updown", new UpDownBuilder());
      RegisterControlBuilder("visualbrush", new VisualBrushBuilder());
      RegisterControlBuilder("list", new ListBuilder());
      RegisterControlBuilder("keyboard", new KeyboardBuilder());
      RegisterControlBuilder("progress", new ProgressBuilder());
      RegisterControlBuilder("tree", new TreeBuilder());
      RegisterControlBuilder("treeitem", new TreeItemBuilder());
      RegisterControlBuilder("repeater", new RepeaterBuilder());

      ServiceScope.Add<ISkinLoader>(this);
    }


    /// <summary>
    /// returns a dictionary with all control builders
    /// </summary>
    /// <value>The builders.</value>
    public Dictionary<string, IControlBuilder> Builders
    {
      get { return _listBuilders; }
    }

    #region public members

    /// <summary>
    /// Loads the skin settings like width/height
    /// </summary>
    public void LoadSkinSettings()
    {
      XmlDocument doc = new XmlDocument();
      doc.Load(String.Format(@"skin\{0}\skin.xml", SkinContext.SkinName));
      XmlNode node = doc.SelectSingleNode("/skin");
      float width = GetFloat(node, "width", 720);
      float height = GetFloat(node, "height", 576);
      SkinContext.Width = width;
      SkinContext.Height = height;
    }

    /// <summary>
    /// Loads the specified window.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="skinFile">The skin file.</param>
    public void Load(Window window, string skinFile)
    {
      Context = new SkinLoaderContext(skinFile);
      try
      {
        ServiceScope.Get<ILogger>().Debug("loading skin file:{0}", skinFile);
        XmlDocument doc = new XmlDocument();
        doc.Load(String.Format(@"skin\{0}\{1}", SkinContext.SkinName, skinFile));
        XmlNode nodeSkin = doc.SelectSingleNode("/skin");
        window.IsOpenedProperty = GetPropertyBool(nodeSkin, window, null, "open", true);
        window.DefaultFocus = GetName(nodeSkin, "defaultfocus");

        window.Result = GetPropertyBool(nodeSkin, window, null, "result", null);
        LoadWindowCloseCommand(window, nodeSkin);
        LoadWindowOpenCommand(window, nodeSkin);
        window.History = GetBoolean(nodeSkin, "history", true);

        XmlNodeList nodes = doc.SelectNodes("/skin/styles/include");
        foreach (XmlNode node in nodes)
        {
          string name = GetName(node, "name");
          if (name != null && name.Length > 0)
          {
            Context.Includes.Add(name);
          }
        }

        ImportModels(window, doc);
        nodes = doc.SelectNodes("/skin/*");
        foreach (XmlNode node in nodes)
        {
          List<Control> controlList = CreateControl(Context, window, node, null, null);
          if (controlList != null)
          {
            foreach (Control c in controlList)
            {
              window.AddControl(c);
            }
          }
        }
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("error loading skin file:{0}", skinFile);
        ServiceScope.Get<ILogger>().Error(ex);
      }
    }

    /// <summary>
    /// Registers a new control builder.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="builder">The builder.</param>
    public void RegisterControlBuilder(string name, IControlBuilder builder)
    {
      _listBuilders[name] = builder;
    }


    /// <summary>
    /// Creates a control.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="container">The container.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    public List<Control> CreateControl(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                       Control parent)
    {
      try
      {
        if (_listBuilders.ContainsKey(node.Name))
        {
          return _listBuilders[node.Name].Create(context, window, node, container, parent);
        }
        else if (node.Name == "models" || node.Name == "styles" || node.Name == "animation")
        {
          //already done...
          return null;
        }
        else if (node.Name == "include")
        {
          return ImportXml(context, window, node, container, parent);
        }
        ServiceScope.Get<ILogger>().Warn("unknown control:{0} found in {1}", node.Name, context.WindowName);
      }
      catch (Exception ex)
      {
        ServiceScope.Get<ILogger>().Error("Unable to load control from {0}", context.WindowName);
        ServiceScope.Get<ILogger>().Error("control: {0}", node.InnerXml.ToString());
        ServiceScope.Get<ILogger>().Error(ex);
      }

      return null;
    }

    #endregion
  }
}
