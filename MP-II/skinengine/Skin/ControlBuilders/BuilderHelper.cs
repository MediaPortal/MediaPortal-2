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
using System.Drawing;
using System.Xml;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Logging;
using MediaPortal.Core.Properties;
using MediaPortal.Core.WindowManager;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.Animations;
using SkinEngine.Commands;
using SkinEngine.Controls;
using SkinEngine.Properties;
using SkinEngine.Scripts;
using SkinEngine.Skin.Layout;
using Font = SkinEngine.Fonts.Font;

namespace SkinEngine.Skin
{
  public class BuilderHelper
  {
    private static SkinLoaderContext _context;

    public static SkinLoaderContext Context
    {
      get { return _context; }
      set { _context = value; }
    }

    protected static List<Control> ImportXml(SkinLoaderContext context, Window window, XmlNode node, Control container,
                                      Control parent)
    {
      List<Control> list = new List<Control>();
      string includeName = GetName(node, "source");
      XmlDocument doc = new XmlDocument();
      XmlTextReader reader = new XmlTextReader(String.Format(@"skin\{0}\{1}", SkinContext.SkinName, includeName));
      doc.Load(reader);
      reader.Close();

      XmlNodeList nodesStyles = doc.SelectNodes("/skin/styles/include");
      foreach (XmlNode nodeStyle in nodesStyles)
      {
        string name = GetName(nodeStyle, "name");
        if (name != null && name.Length > 0)
        {
          context.Includes.Add(name);
        }
      }

      ISkinLoader loader = ServiceScope.Get<ISkinLoader>();
      XmlNodeList nodes = doc.SelectNodes("/skin/*");
      foreach (XmlNode nodeControl in nodes)
      {
        List<Control> controls = loader.CreateControl(context, window, nodeControl, container, parent);
        if (controls != null)
        {
          foreach (Control c in controls)
          {
            list.Add(c);
          }
        }
      }
      return list;
    }

    public Style LoadStyle(Window window, Control container, string styleName, Control parent)
    {
      Style style = new Style(parent);
      style.Container = container;
      style.Window = window;

      XmlNode styleNode;
      XmlNodeList nodes = GetStyle(styleName, out styleNode);
      if (nodes != null)
      {
        style.Move = GetVector4(styleNode, "move", new Vector4(0, 1, 0, 1));
        style.WrapProperty = GetPropertyBool(styleNode, window, style, "wrap", false);
        style.AlphaMask = GetVector4(styleNode, "alphamask", new Vector4(1, 1, 1, 1));
        style.AlphaGradient = LoadAlphaGradient(styleNode, window);
        style.Name = GetName(styleNode);
        style.DisplayNameProperty = new Property(GetName(styleNode, "displayname"));
        LoadClipPlane(window, styleNode, style, "clip");

        LoadAnimations(window, styleNode, container);
        ISkinLoader loader = ServiceScope.Get<ISkinLoader>();
        foreach (XmlNode node in nodes)
        {
          List<Control> controls = loader.CreateControl(Context, window, node, container, parent);
          if (controls != null)
          {
            foreach (Control c in controls)
            {
              if (c.Name.Length > 0)
              {
                window.AddNamedControl(c);
              }
              style.Controls.Add(c);
            }
          }
        }
        string layout = GetName(styleNode, "layout");
        DoLayout(style, layout);
      }
      return style;
    }

    public Style LoadStyle(Window window, Control container, XmlNode styleRoot, Control parent)
    {
      Style style = new Style(parent);
      LoadAnimations(window, styleRoot, style);
      style.Container = container;
      Property position = GetPropertyVector3(styleRoot, style, "position", new Vector3(0, 0, 0));
      if (container != null)
      {
        style.PositionProperty = new PositionDependency(container.PositionProperty, position);
      }
      else
      {
        style.PositionProperty = position;
      }

      style.Move = GetVector4(styleRoot, "move", new Vector4(0, 1, 0, 1));
      style.Wrap = GetBoolean(styleRoot, "wrap", false);
      style.AlphaMask = GetVector4(styleRoot, "alphamask", new Vector4(1, 1, 1, 1));
      style.AlphaGradient = LoadAlphaGradient(styleRoot, window);
      style.Window = window;
      style.Name = GetName(styleRoot);
      style.DisplayNameProperty = new Property(GetName(styleRoot, "displayname"));
      LoadClipPlane(window, styleRoot, style, "clip");
      XmlNodeList nodes = styleRoot.ChildNodes;
      ISkinLoader loader = ServiceScope.Get<ISkinLoader>();
      if (nodes != null)
      {
        foreach (XmlNode node in nodes)
        {
          List<Control> controls = loader.CreateControl(Context, window, node, container, style);
          if (controls != null)
          {
            foreach (Control c in controls)
            {
              if (c.Name.Length > 0)
              {
                window.AddNamedControl(c);
              }
              style.Controls.Add(c);
            }
          }
        }
      }
      string layout = GetName(styleRoot, "layout");
      DoLayout(style, layout);
      return style;
    }

    public StylesCollection LoadStyles(Window window, Control container, string styleName, Control parent)
    {
      StylesCollection collection = new StylesCollection(parent);
      collection.Container = container;
      collection.Name = styleName;

      foreach (string include in Context.Includes)
      {
        if (!Context.Styles.ContainsKey(include))
        {
          XmlDocument doc = new XmlDocument();
          string fileName = String.Format(@"skin\{0}\styles\{1}", SkinContext.SkinName, include);
          ServiceScope.Get<ILogger>().Debug("skin: loading style:{0}", fileName);
          XmlTextReader reader = new XmlTextReader(fileName);
          doc.Load(reader);
          reader.Close();
          Context.Styles[include] = doc;
        }
        XmlNodeList nodes = Context.Styles[include].SelectNodes("/styles/styles");
        foreach (XmlNode node in nodes)
        {
          if (node.Attributes["name"].Value == styleName)
          {
            foreach (XmlNode child in node.ChildNodes)
            {
              collection.Styles.Add(LoadStyle(window, container, child, collection));
            }
            return collection;
          }
        }
      }
      return null;
    }

    protected XmlNodeList GetStyle(string styleName, out XmlNode styleNode)
    {
      styleNode = null;
      foreach (string include in Context.Includes)
      {
        if (!Context.Styles.ContainsKey(include))
        {
          string fileName = String.Format(@"skin\{0}\styles\{1}", SkinContext.SkinName, include);
          ServiceScope.Get<ILogger>().Debug("skin: loading style:{0}", fileName);
          XmlDocument doc = new XmlDocument();
          XmlTextReader reader = new XmlTextReader(fileName);
          doc.Load(reader);
          reader.Close();
          Context.Styles[include] = doc;
        }
        XmlNodeList nodes = Context.Styles[include].SelectNodes("/styles/style");
        foreach (XmlNode node in nodes)
        {
          if (node.Attributes["name"].Value == styleName)
          {
            styleNode = node;
            return node.ChildNodes;
          }
        }
      }
      return null;
    }

    #region helper methods

    protected static Rectangle GetRectangle(XmlNode node, string attributeName)
    {
      Rectangle r = new Rectangle();
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return r;
      }
      string position = attrib.Value;
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length < 4)
      {
        return r;
      }
      r.X = (int)GetFloat(coords[0]);
      r.Y = (int)GetFloat(coords[1]);
      r.Width = (int)GetFloat(coords[2]);
      r.Height = (int)GetFloat(coords[3]);
      return r;
    }

    protected static Vector4 GetVector4(XmlNode node, string attributeName, Vector4 defaultvector)
    {
      Vector4 vec = defaultvector;
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return vec;
      }
      string position = attrib.Value;
      if (position == null)
      {
        return vec;
      }
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = GetFloat(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = GetFloat(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.Z = GetFloat(coords[2]);
      }
      if (coords.Length > 3)
      {
        vec.W = GetFloat(coords[3]);
      }
      return vec;
    }

    protected static IVector3Property GetVector3(XmlNode node, string attributeName, Control container)
    {
      Vector3 vec = new Vector3(0, 0, 0);
      Vector3Property prop = new Vector3Property(vec);
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return prop;
      }
      string position = attrib.Value;
      if (position == null)
      {
        return prop;
      }
      if (position.ToLower().StartsWith("#script:"))
      {
        string scriptName = position.Substring("#script:".Length);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          return new Vector3Property((IVector3Property)ScriptManager.Instance.GetScript(scriptName), container);
        }
      }
      string[] coords = position.Split(new char[] { ',' });
      if (coords.Length > 0)
      {
        vec.X = GetFloat(coords[0]);
      }
      if (coords.Length > 1)
      {
        vec.Y = GetFloat(coords[1]);
      }
      if (coords.Length > 2)
      {
        vec.Z = GetFloat(coords[2]);
      }
      prop.Vector = vec;
      return prop;
    }

    protected static float GetFloat(XmlNode node, string attributeName, float defaultValue)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return defaultValue;
      }
      string floatString = attrib.Value;
      if (floatString == null)
      {
        return defaultValue;
      }
      float result;
      float test = 12.03f;
      string comma = test.ToString();
      bool replaceCommas = (comma.IndexOf(",") >= 0);
      if (replaceCommas)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      if (float.TryParse(floatString, out result))
      {
        return result;
      }
      return defaultValue;
    }

    protected static float GetFloat(string floatString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      bool replaceCommas = (comma.IndexOf(",") >= 0);
      if (replaceCommas)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      float f;
      float.TryParse(floatString, out f);
      return f;
    }

    protected static bool GetBoolean(XmlNode node, string name, bool defaultValue)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(name);
      if (attrib == null)
      {
        return defaultValue;
      }
      string colorString = attrib.Value;
      if (colorString == null)
      {
        return defaultValue;
      }
      if (colorString == "true")
      {
        return true;
      }
      if (colorString == "false")
      {
        return false;
      }
      return defaultValue;
    }

    protected static Font.Align GetAlignment(XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("align");
      if (attrib == null)
      {
        return Font.Align.Left;
      }
      string colorString = attrib.Value;
      if (colorString == null)
      {
        return Font.Align.Left;
      }
      if (colorString == "right")
      {
        return Font.Align.Right;
      }
      if (colorString == "center")
      {
        return Font.Align.Center;
      }
      return Font.Align.Left;
    }

    protected static string GetName(XmlNode node)
    {
      return GetName(node, "name");
    }

    protected static string GetName(XmlNode node, string attribute)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attribute);
      if (attrib == null)
      {
        return "";
      }
      string name = attrib.Value;
      if (name == null)
      {
        return "";
      }
      return name;
    }

    /// <summary>
    /// Gets the container attribtue from the xml node.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <returns>Container control or null if no container was found</returns>
    protected static Control GetContainer(Window window, XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("container");
      if (attrib == null)
      {
        return null;
      }
      string name = attrib.Value;
      if (name == null)
      {
        return null;
      }
      return window.GetControlByName(name);
    }

    /// <summary>
    /// Gets the color from the xml node and returns the ColorValue
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>colorValue</returns>
    protected static ColorValue GetColor(XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("color");
      if (attrib == null)
      {
        return ColorValue.FromColor(Color.White);
      }
      string colorString = attrib.Value;
      if (colorString == null)
      {
        return ColorValue.FromColor(Color.White);
      }

      if (SkinContext.Theme.HasColor(colorString))
      {
        return SkinContext.Theme.GetColor(colorString);
      }
      string[] parts = colorString.Split(new char[] { ',' });
      if (parts.Length < 3)
      {
        return ColorValue.FromColor(Color.White);
      }
      float a, r, g, b;
      a = 255.0f;
      r = Convert.ToInt32(parts[0]);
      r /= 255.0f;
      g = Convert.ToInt32(parts[1]);
      g /= 255.0f;
      b = Convert.ToInt32(parts[2]);
      b /= 255.0f;
      if (parts.Length == 4)
      {
        a = Convert.ToInt32(parts[3]);
      }
      a /= 255.0f;
      if (a < 0)
      {
        a = 0;
      }
      if (a > 1)
      {
        a = 1.0f;
      }

      if (r < 0)
      {
        r = 0;
      }
      if (r > 1)
      {
        r = 1.0f;
      }

      if (g < 0)
      {
        g = 0;
      }
      if (g > 1)
      {
        g = 1.0f;
      }

      if (b < 0)
      {
        b = 0;
      }
      if (b > 1)
      {
        b = 1.0f;
      }
      return new ColorValue(r, g, b, a);
    }

    /// <summary>
    /// Gets the boolean condition from the xml node
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="control">The control.</param>
    /// <param name="attributeName">The name.</param>
    /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
    /// <returns>IBooleanProperty</returns>
    protected static IBooleanProperty GetBooleanCondition(XmlNode node, Control control, string attributeName,
                                                   bool defaultValue)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return new BooleanProperty(defaultValue);
      }
      string attribtueValue = attrib.Value;
      return GetBooleanProperty(attribtueValue, defaultValue);
    }

    /// <summary>
    /// Gets the boolean property from string .
    /// </summary>
    /// <param name="attributeName">The string.</param>
    /// <param name="defaultValue">default value to return when property is not found</param>
    /// <returns>IBooleanProperty</returns>
    protected static IBooleanProperty GetBooleanProperty(string attributeName, bool defaultValue)
    {
      IBooleanProperty prop = new BooleanProperty(defaultValue);
      if (attributeName == null)
      {
        return prop;
      }
      if (attributeName.Length == 0)
      {
        return prop;
      }
      if (attributeName.ToLower() == "true")
      {
        return new BooleanProperty(true);
      }
      else if (attributeName.ToLower() == "false")
      {
        return new BooleanProperty(false);
      }
      else if (attributeName.ToLower().StartsWith("#script:"))
      {
        string scriptName = attributeName.Substring("#script:".Length);
        string param = GetScriptParam(ref scriptName);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          IBooleanProperty property = (IBooleanProperty)ScriptManager.Instance.GetScript(scriptName);
          if (param.Length == 0)
          {
            return property;
          }
          return new BooleanPropertyControl(property, param);
        }
      }
      else
      {
        return new BooleanReflectionProperty(attributeName);
      }
      return prop;
    }

    /// <summary>
    /// Gets the float property.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attributeName">Name of the attribute.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <returns></returns>
    protected static IFloatProperty GetFloatProperty(XmlNode node, string attributeName, float defaultValue)
    {
      IFloatProperty prop = new FloatProperty(defaultValue);
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return prop;
      }
      string attribtueValue = attrib.Value;
      if (attribtueValue == null)
      {
        return prop;
      }
      if (attribtueValue.Length == 0)
      {
        return prop;
      }
      if (attribtueValue.ToLower().StartsWith("#script:"))
      {
        string scriptName = attribtueValue.Substring("#script:".Length);
        string param = GetScriptParam(ref scriptName);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          IFloatProperty property = (IFloatProperty)ScriptManager.Instance.GetScript(scriptName);
          if (param.Length == 0)
          {
            return property;
          }
          return new FloatPropertyControl(property, param);
        }
      }
      else
      {
        return new FloatReflectionProperty(attribtueValue);
      }
      return prop;
    }

    /// <summary>
    /// Gets the image source from the xml node
    /// </summary>
    /// <param name="window"></param>
    /// <param name="node">The node.</param>
    /// <returns>ILabelProperty or null if no source was found</returns>
    /// <param name="attributeName"></param>
    protected static ILabelProperty GetSource(Window window, XmlNode node, string attributeName)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        return null;
      }
      string labelString = attrib.Value;
      if (labelString == null)
      {
        return null;
      }
      if (labelString.Length == 0)
      {
        return null;
      }
      else if (labelString.ToLower().StartsWith("#script:"))
      {
        string scriptName = labelString.Substring("#script:".Length);
        string paramName = GetScriptParam(ref scriptName);

        if (ScriptManager.Instance.Contains(scriptName))
        {
          if (paramName.Length > 0)
          {
            ILabelProperty prop = (ILabelProperty)ScriptManager.Instance.GetScript(scriptName);
            return new LabelProperty(window, prop, paramName);
          }
          else
          {
            return (ILabelProperty)ScriptManager.Instance.GetScript(scriptName);
          }
        }
      }
      else
      {
        int pos = labelString.IndexOf(".");
        if (pos > 0 && pos < labelString.Length - 4)
        {
          return new LabelReflectionProperty(labelString);
        }
      }
      return new LabelProperty(labelString);
    }

    /// <summary>
    /// Gets the label from the xml node.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="node">The node.</param>
    /// <returns>ILabelProperty or null if no label was found</returns>
    protected static ILabelProperty GetLabelCondition(Window window, XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("label");
      if (attrib == null)
      {
        return null;
      }
      string labelString = attrib.Value;
      if (labelString == null)
      {
        return null;
      }
      if (labelString.Length == 0)
      {
        return null;
      }
      else if (labelString.ToLower().StartsWith("#script:"))
      {
        string scriptName = labelString.Substring("#script:".Length);
        string paramName = GetScriptParam(ref scriptName);

        if (ScriptManager.Instance.Contains(scriptName))
        {
          if (paramName.Length > 0)
          {
            ILabelProperty prop = (ILabelProperty)ScriptManager.Instance.GetScript(scriptName);
            return new LabelProperty(window, prop, paramName);
          }
          else
          {
            return (ILabelProperty)ScriptManager.Instance.GetScript(scriptName);
          }
        }
      }
      else if (labelString.IndexOf(".") > 0)
      {
        return new LabelReflectionProperty(labelString);
      }
      return new LabelProperty(labelString);
    }

    /// <summary>
    /// load and returns the ListItem property from the xml node
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>IListItemProperty or null if no listitem property was found</returns>
    protected static IListItemProperty GetItemProperty(XmlNode node)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("item");
      if (attrib == null)
      {
        return null;
      }
      string labelString = attrib.Value;
      if (labelString == null)
      {
        return null;
      }
      if (labelString.Length == 0)
      {
        return null;
      }
      else if (labelString.ToLower().StartsWith("#script:"))
      {
        string scriptName = labelString.Substring("#script:".Length);
        string paramName = GetScriptParam(ref scriptName);
        int index = 0;
        if (paramName != "")
        {
          Int32.TryParse(paramName, out index);
        }
        if (ScriptManager.Instance.Contains(scriptName))
        {
          IListItemProperty prop = (IListItemProperty)ScriptManager.Instance.GetScript(scriptName);
          return new ListItemProperty(index, prop);
        }
      }
      return null;
    }

    protected static void LoadWindowCloseCommand(Window window, XmlNode node)
    {
      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window, node, null, "closecommand", "closecommandparameter", "closecommandresult", out cmd, out param, out cmdResult);
      window.CloseCommand = cmd;
      window.CloseCommandParameter = param;
    }
    protected static void LoadWindowOpenCommand(Window window, XmlNode node)
    {
      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window, node, null, "opencommand", "opencommandparameter", "opencommandresult", out cmd, out param, out cmdResult);
      window.OpenCommand = cmd;
      window.OpenCommandParameter = param;
    }

    /// <summary>
    /// Loads the command and command parameter from the xmlnode.
    /// If command is found it is stored in the control.Command and control.CommandParameter
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The button.</param>
    protected static void LoadCommand(Window window, XmlNode node, Control control)
    {
      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window, node, control, "command", "parameter", "commandresult", out cmd, out param, out cmdResult);
      control.Command = cmd;
      control.CommandParameter = param;
      control.CommandResult = cmdResult;
    }

    /// <summary>
    /// Loads the command and command parameter from the xmlnode.
    /// If command is found it is stored in the control.ContextMenuCommand and control.ContextMenuCommandParameter
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The button.</param>
    protected static void LoadContextMenu(Window window, XmlNode node, Control control)
    {
      ICommand cmd;
      ICommandParameter param;
      ICommandResult cmdResult;
      GetCommand(window, node, control, "contextmenu", "contextmenuparameter", "contextmenuresult", out cmd, out param, out cmdResult);
      control.ContextMenuCommand = cmd;
      control.ContextMenuCommandParameter = param;
    }

    protected static void GetCommand(Window window, XmlNode node, Control control, string attrNameCmd, string attrNameParam, string attrNameRes, out ICommand cmd, out ICommandParameter param, out ICommandResult cmdResult)
    {
      cmd = null;
      param = null;
      cmdResult = null;
      cmd = GetCommand(node, attrNameCmd);
      if (cmd != null)
      {
        return;
      }
      XmlNode cmdLabel = node.Attributes.GetNamedItem(attrNameCmd);
      if (cmdLabel == null)
      {
        return;
      }
      string cmdString = cmdLabel.Value;
      if (cmdString == null)
      {
        return;
      }
      if (cmdString.Length == 0)
      {
        return;
      }
      cmd = new ReflectionCommand(control, window, cmdString);

      cmdLabel = node.Attributes.GetNamedItem(attrNameParam);
      if (cmdLabel != null)
      {
        cmdString = cmdLabel.Value;
        if (cmdString != null)
        {
          if (cmdString.Length != 0)
          {
            param = new StringParameter(cmdString);
          }
        }
      }
      cmdLabel = node.Attributes.GetNamedItem(attrNameRes);
      if (cmdLabel != null)
      {
        cmdString = cmdLabel.Value;
        if (cmdString != null)
        {
          if (cmdString.Length != 0)
          {
            cmdResult = new ReflectionCommandResult(control, window, cmdString);
          }
        }
      }
    }

    /// <summary>
    /// Gets the ICommand from the xmlnode.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>ICommand or null if no command was found</returns>
    /// <param name="name"></param>
    protected static ICommand GetCommand(XmlNode node, string name)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(name);
      if (attrib == null)
      {
        return null;
      }
      string cmdString = attrib.Value;
      if (cmdString == null)
      {
        return null;
      }
      if (cmdString.Length == 0)
      {
        return null;
      }
      else if (cmdString.ToLower().StartsWith("#script:"))
      {
        string scriptName = cmdString.Substring("#script:".Length);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          return (ICommand)ScriptManager.Instance.GetScript(scriptName);
        }
      }
      return null;
    }

    /// <summary>
    /// Loads all animations for a control
    /// Animations loaded will be stored in control.Animations
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The control.</param>
    protected static void LoadAnimations(Window window, XmlNode node, Control control)
    {
      XmlNodeList nodes = node.ChildNodes;
      foreach (XmlNode childNode in nodes)
      {
        if (childNode.Name == "animationgroup")
        {
          IAnimation animation = LoadAnimationGroup(window, childNode, control);
          if (animation != null)
          {
            control.Animations.Add(animation);
          }
        }
        if (childNode.Name == "animation")
        {
          IAnimation animation = LoadAnimation(window, childNode, control);
          if (animation != null)
          {
            control.Animations.Add(animation);
          }
        }
      }
    }

    protected static IAnimation LoadAnimationGroup(Window window, XmlNode node, Control control)
    {
      XmlNode nodeTrigger = node.Attributes.GetNamedItem("trigger");
      if (nodeTrigger == null)
      {
        return null;
      }
      if (nodeTrigger.Value == null)
      {
        return null;
      } ITrigger trigger = null;
      bool negate = false;
      string triggerValue = nodeTrigger.Value;
      if (triggerValue.IndexOf("!") >= 0)
      {
        negate = true;
        triggerValue = triggerValue.Substring(1);
      }
      if (triggerValue.StartsWith("#script:"))
      {
        Property triggerProperty = GetPropertyBool(window, control, triggerValue, true);
        trigger = new ScriptTrigger(control, triggerProperty, negate);
      }
      if (triggerValue.IndexOf("onwindowopen") >= 0)
      {
        trigger = new WindowOpenTrigger(control, negate);
      }
      if (triggerValue.IndexOf("onwindowclose") >= 0)
      {
        trigger = new WindowOpenTrigger(control, true);
      }
      if (triggerValue.IndexOf("hasmousefocus") >= 0)
      {
        trigger = new MouseFocusTrigger(control, negate);
      }
      else if (triggerValue.IndexOf("hasfocus") >= 0)
      {
        int pos = triggerValue.IndexOf("(");
        if (pos > 0)
        {
          string name = triggerValue.Substring(pos + 1, triggerValue.Length - pos - 2);
          trigger = new FocusNamedControlTrigger(name, negate);
        }
        else
        {
          trigger = new FocusTrigger(control, negate);
        }
      }
      if (triggerValue.IndexOf("isvisible") >= 0)
      {
        trigger = new VisibleTrigger(control, negate);
      }
      //if (triggerValue.IndexOf("overlaps") >= 0)
      //trigger = new OverlapsTrigger(window, control, controlName, negate);
      if (trigger == null)
      {
        return null;
      }
      float duration = GetFloat(node, "duration", 300);
      float delay = GetFloat(node, "delay", 0);
      int repeat = (int)GetFloat(node, "repeat", 0);

      AnimationGroup group = new AnimationGroup(trigger, new TimeSpan(0, 0, 0, 0, (int)duration));
      group.Repeat = repeat;
      group.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
      XmlNodeList nodes = node.ChildNodes;
      foreach (XmlNode childNode in nodes)
      {
        if (childNode.Name == "animation")
        {
          IAnimation animation = LoadAnimation(window, childNode, control);
          if (animation != null)
          {
            group.Animations.Add(animation);
          }
        }
      }
      if (group.Animations.Count > 0) return group;
      return null;
    }
    /// <summary>
    /// Loads the animation from the xmlnode.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The control.</param>
    /// <returns>IAnimation if animation was found otherwise null</returns>
    protected static IAnimation LoadAnimation(Window window, XmlNode node, Control control)
    {
      XmlNode nodeTrigger = node.Attributes.GetNamedItem("trigger");
      XmlNode nodeType = node.Attributes.GetNamedItem("type");
      XmlNode nodeTween = node.Attributes.GetNamedItem("tween");
      XmlNode nodeEase = node.Attributes.GetNamedItem("ease");
      XmlNode nodeAxis = node.Attributes.GetNamedItem("axis");
      XmlNode nodeCenter = node.Attributes.GetNamedItem("center");
      if (nodeTrigger == null)
      {
        return null;
      }
      if (nodeTrigger.Value == null)
      {
        return null;
      }
      if (nodeType == null)
      {
        return null;
      }
      if (nodeType.Value == null)
      {
        return null;
      }
      ITrigger trigger = null;
      bool negate = false;
      string triggerValue = nodeTrigger.Value;
      if (triggerValue.IndexOf("!") >= 0)
      {
        negate = true;
        triggerValue = triggerValue.Substring(1);
      }
      if (triggerValue.StartsWith("#script:"))
      {
        Property triggerProperty = GetPropertyBool(window, control, triggerValue, true);
        trigger = new ScriptTrigger(control, triggerProperty, negate);
      }
      if (triggerValue.IndexOf("onwindowopen") >= 0)
      {
        trigger = new WindowOpenTrigger(control, negate);
      }
      if (triggerValue.IndexOf("onwindowclose") >= 0)
      {
        trigger = new WindowOpenTrigger(control, true);
      }
      if (triggerValue.IndexOf("hasmousefocus") >= 0)
      {
        trigger = new MouseFocusTrigger(control, negate);
      }
      else if (triggerValue.IndexOf("hasfocus") >= 0)
      {
        int pos = triggerValue.IndexOf("(");
        if (pos > 0)
        {
          string name = triggerValue.Substring(pos + 1, triggerValue.Length - pos - 2);
          trigger = new FocusNamedControlTrigger(name, negate);
        }
        else
        {
          trigger = new FocusTrigger(control, negate);
        }
      }
      if (triggerValue.IndexOf("isvisible") >= 0)
      {
        trigger = new VisibleTrigger(control, negate);
      }
      //if (triggerValue.IndexOf("overlaps") >= 0)
      //trigger = new OverlapsTrigger(window, control, controlName, negate);
      if (trigger == null)
      {
        return null;
      }
      float duration = GetFloat(node, "duration", 300);
      float delay = GetFloat(node, "delay", 0);

      Tweener tweener = new LinearTweener();
      if (nodeTween != null)
      {
        if (nodeTween.Value == "linear")
        {
          tweener = new LinearTweener();
        }
        if (nodeTween.Value == "quad")
        {
          tweener = new QuadTweener();
        }
        if (nodeTween.Value == "cubic")
        {
          tweener = new CubicTweener();
        }
        if (nodeTween.Value == "sine")
        {
          tweener = new SineTweener();
        }
        if (nodeTween.Value == "circle")
        {
          tweener = new CircleTweener();
        }
        if (nodeTween.Value == "back")
        {
          tweener = new BackTweener();
        }
        if (nodeTween.Value == "bounce")
        {
          tweener = new BounceTweener();
        }
        if (nodeTween.Value == "eleastic")
        {
          tweener = new ElasticTweener();
        }
      }
      if (nodeEase != null)
      {
        if (nodeEase.Value == "easein")
        {
          tweener.Easing = TweenerType.EaseIn;
        }
        if (nodeEase.Value == "easeout")
        {
          tweener.Easing = TweenerType.EaseOut;
        }
        if (nodeEase.Value == "easeinout")
        {
          tweener.Easing = TweenerType.EaseInOut;
        }
      }
      if (nodeType.Value == "zoom")
      {
        float from = GetFloat(node, "from", 0.0f);
        float to = GetFloat(node, "to", 1.0f);
        if (nodeCenter != null)
        {
          Vector3 center = GetVector3(node, "center", null).Vector;
          ZoomAnimation animation =
            new ZoomAnimation(trigger, from, to, new TimeSpan(0, 0, 0, 0, (int)duration), center);
          animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
          animation.Tweener = tweener;
          return animation;
        }
        else
        {
          ZoomAnimation animation =
            new ZoomAnimation(trigger, from, to, new TimeSpan(0, 0, 0, 0, (int)duration), control);
          animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
          animation.Tweener = tweener;
          return animation;
        }
      }
      if (nodeType.Value == "rotate")
      {
        float from = GetFloat(node, "from", 0.0f);
        float to = GetFloat(node, "to", 1.0f);
        Axis axis = Axis.Z;
        if (nodeAxis != null)
        {
          if (nodeAxis.Value == "x")
          {
            axis = Axis.X;
          }
          if (nodeAxis.Value == "y")
          {
            axis = Axis.Y;
          }
          if (nodeAxis.Value == "z")
          {
            axis = Axis.Z;
          }
        }
        if (nodeCenter != null)
        {
          Vector3 center = GetVector3(node, "center", null).Vector;
          RotateAnimation animation =
            new RotateAnimation(trigger, axis, from, to, new TimeSpan(0, 0, 0, 0, (int)duration), center);
          animation.Tweener = tweener;
          animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
          return animation;
        }
        else
        {
          RotateAnimation animation =
            new RotateAnimation(trigger, axis, from, to, new TimeSpan(0, 0, 0, 0, (int)duration), control);
          animation.Tweener = tweener;
          animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
          return animation;
        }
      }
      if (nodeType.Value == "fade")
      {
        float from = GetFloat(node, "from", 0.0f);
        float to = GetFloat(node, "to", 1.0f);
        AlphaAnimation animation = new AlphaAnimation(trigger, from, to, new TimeSpan(0, 0, 0, 0, (int)duration));
        animation.Tweener = tweener;
        animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
        return animation;
      }

      if (nodeType.Value == "slide")
      {
        Vector3 from = GetVector3(node, "from", null).Vector;
        Vector3 to = GetVector3(node, "to", null).Vector;
        SlideAnimation animation = new SlideAnimation(trigger, from, to, new TimeSpan(0, 0, 0, 0, (int)duration));
        animation.Tweener = tweener;
        animation.Delay = new TimeSpan(0, 0, 0, 0, (int)delay);
        return animation;
      }
      return null;
    }

    /// <summary>
    /// Loads the clip planes from the xml node.
    /// If clipplanes are found they are stored in the control
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="node">The node.</param>
    /// <param name="control">The control.</param>
    /// <param name="attributeName">Name of the attribute.</param>
    protected static void LoadClipPlane(Window window, XmlNode node, Control control, string attributeName)
    {
      XmlNode attrib = node.Attributes.GetNamedItem("clip");
      if (attrib == null)
      {
        return;
      }
      string clipValue = attrib.Value;
      if (clipValue == null)
      {
        return;
      }
      if (clipValue.Length == 0)
      {
        return;
      }
      string[] parts = clipValue.Split(new char[] { ',' });
      if (parts.Length != 4)
      {
        return;
      }
      int x, y, w, h;
      Int32.TryParse(parts[0], out x);
      Int32.TryParse(parts[1], out y);
      Int32.TryParse(parts[2], out w);
      Int32.TryParse(parts[3], out h);
      control.ClipPlane1Enabled = true;
      control.ClipPlane1 = new Rectangle(x, y, w, h);
    }

    /// <summary>
    /// Load the alpha gradient from the xml node and returns an AlphaMask
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>AlphaMask or null if no gradient is specified</returns>
    protected static AlphaMask LoadAlphaGradient(XmlNode node, Window window)
    {
      AlphaMask mask = null;
      XmlNode nodeMask = node.SelectSingleNode("opacitymask");
      if (nodeMask != null)
      {
        mask = new AlphaMask();
        mask.Window = window;
        XmlNodeList nodeList = nodeMask.SelectNodes("gradientstop");
        foreach (XmlNode nodeGradient in nodeList)
        {
          float offset = GetFloat(nodeGradient, "offset", 0);
          float color = GetFloat(nodeGradient, "color", 0);
          color /= 255.0f;
          mask.GradientStops.Add(new GradientStop(offset, color));
        }

        mask.IsVisibleProperty = GetPropertyBool(nodeMask, window, mask, "visible", true);
        ContentManager.Add(mask);
      }
      return mask;
    }

    /// <summary>
    /// Imports the models.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="doc">The doc.</param>
    protected static void ImportModels(Window window, XmlDocument doc)
    {
      XmlNodeList nodeList = doc.SelectNodes("skin/models/model");
      foreach (XmlNode nodeModel in nodeList)
      {
        string modelName = GetName(nodeModel);
        string className = GetName(nodeModel, "class");
        string assembly = GetName(nodeModel, "assembly");
        if (!ModelManager.Instance.Contains(assembly, className))
        {
          ModelManager.Instance.Load(assembly, className);
        }
        window.AddModel(modelName, ModelManager.Instance.GetModel(assembly, className));
      }
    }

    /// <summary>
    /// Layouts all subcontrols of the group
    /// </summary>
    /// <param name="group">The group.</param>
    /// <param name="layout">The layout.</param>
    protected static ILayout DoLayout(Group group, string layout)
    {
      if (layout == "stack")
      {
        return new StackLayout(group);
      }

      if (layout == "flow")
      {
        return new FlowLayout(group);
      }
      return null;
    }

    #endregion

    protected static Property GetPropertyBool(XmlNode node, IWindow window, Control control, string attributeName, object defaultValue)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        if (defaultValue == null)
        {
          return null;
        }
        return new Property(defaultValue);
      }
      string attribtueValue = attrib.Value;
      string[] parts = attribtueValue.Split(new char[] { '+' });
      if (parts.Length == 1)
        return GetPropertyBool(window, control, attribtueValue, defaultValue);

      DependencyAnd dep = new DependencyAnd();
      for (int i = 0; i < parts.Length; ++i)
      {
        Property p = GetPropertyBool(window, control, parts[i], defaultValue);
        if (i == 0) dep.DependencyObject = p;
        else dep.Add(p);
      }
      return dep;
    }

    protected static Property GetPropertyBool(IWindow window, Control control, string attributeName, object defaultValue)
    {
      Property prop = new Property(defaultValue);
      if (attributeName == null)
      {
        return prop;
      }
      if (attributeName.Length == 0)
      {
        return prop;
      }
      if (attributeName.ToLower() == "true")
      {
        return new Property(true);
      }
      else if (attributeName.ToLower() == "false")
      {
        return new Property(false);
      }
      else if (attributeName.ToLower().StartsWith("#script:"))
      {
        string scriptName = attributeName.Substring("#script:".Length);
        string param = GetScriptParam(ref scriptName);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          IScriptProperty scriptProp = (IScriptProperty)ScriptManager.Instance.GetScript(scriptName);

          if (param.IndexOf(".") >= 0)
            return new ReflectionDependency(window, control, scriptProp, param);
          else
          {
            Property p = scriptProp.Get(control, param);
            return p;
          }
        }
      }
      else
      {
        Property newProp = new ReflectionDependency(window, control, attributeName);
        newProp.Name = attributeName;
        return newProp;
      }
      return prop;
    }


    protected static Property GetPropertyVector3(XmlNode node, Control control, string attributeName, object defaultValue)
    {
      XmlNode attrib = node.Attributes.GetNamedItem(attributeName);
      if (attrib == null)
      {
        if (defaultValue == null)
        {
          return null;
        }
        return new Property(defaultValue);
      }
      string attribtueValue = attrib.Value;
      return GetPropertyVector3(control, attribtueValue, defaultValue);
    }

    protected static Property GetPropertyVector3(Control control, string attributeName, object defaultValue)
    {
      Property prop = new Property(defaultValue);
      if (attributeName == null)
      {
        return prop;
      }
      if (attributeName.Length == 0)
      {
        return prop;
      }
      else if (attributeName.ToLower().StartsWith("#script:"))
      {
        string scriptName = attributeName.Substring("#script:".Length);
        string param = GetScriptParam(ref scriptName);
        if (ScriptManager.Instance.Contains(scriptName))
        {
          IScriptProperty scriptProp = (IScriptProperty)ScriptManager.Instance.GetScript(scriptName);
          return scriptProp.Get(control, param);
        }
      }
      else
      {
        Vector3 vec = new Vector3(0, 0, 0);
        string[] coords = attributeName.Split(new char[] { ',' });
        if (coords.Length > 0)
        {
          vec.X = GetFloat(coords[0]);
        }
        if (coords.Length > 1)
        {
          vec.Y = GetFloat(coords[1]);
        }
        if (coords.Length > 2)
        {
          vec.Z = GetFloat(coords[2]);
        }
        prop.SetValue(vec);
      }
      return prop;
    }

    static string GetScriptParam(ref string scriptName)
    {
      string param = "";
      int pos = scriptName.IndexOf("(");
      if (pos >= 0)
      {
        param = scriptName.Substring(pos + 1, scriptName.Length - pos - 2);
        param = param.Replace("$x", Context.Index.ToString());
        scriptName = scriptName.Substring(0, pos);
      }
      return param;
    }
  }
}
