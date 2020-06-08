#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  public abstract class ScraperNode
  {
    protected static ILogger Logger => ServiceRegistration.Get<ILogger>();
    private static Dictionary<string, Type> TypeCache;

    protected XmlNode xmlNode;
    protected List<ScraperNode> children;
    protected ScraperNodeAttribute nodeSettings;

    #region Properties

    public string NodeType
    {
      get
      {
        foreach (Attribute currAttr in this.GetType().GetCustomAttributes(true))
          if (currAttr is ScraperNodeAttribute)
            return ((ScraperNodeAttribute)currAttr).NodeName;
        return null;
      }
    }
    public string Name { get; protected set; }
    public virtual bool LoadSuccess { get; protected set; }
    public ScriptableScraper Context { get; private set; }

    #endregion

    #region Methods

    public ScraperNode(XmlNode xmlNode, ScriptableScraper context)
    {
      this.xmlNode = xmlNode;
      children = new List<ScraperNode>();
      this.Context = context;
      LoadSuccess = LoadChildren();

      // try to load our node attribute
      foreach (Attribute currAttr in this.GetType().GetCustomAttributes(true))
        if (currAttr is ScraperNodeAttribute)
        {
          nodeSettings = (ScraperNodeAttribute)currAttr;
          continue;
        }

      if (nodeSettings.LoadNameAttribute)
      {

        // Load attributes
        foreach (XmlAttribute attr in xmlNode.Attributes)
        {
          switch (attr.Name)
          {
            case "name":
              Name = attr.Value;
              break;
          }
        }

        // Validate NAME attribute
        if (Name == null)
        {
          Logger.Error("ScriptableScraperProvider: Missing NAME attribute on: {0}", xmlNode.OuterXml);
          LoadSuccess = false;
          return;
        }

        // if it's a bad variable name we fail as well
        if (Name.Contains(" "))
        {
          Logger.Error("ScriptableScraperProvider: Invalid NAME attribute (no spaces allowed) \"{0}\" for {}", Name, xmlNode.OuterXml);
          LoadSuccess = false;
          return;
        }
      }
    }

    public abstract void Execute(Dictionary<string, string> variables);

    protected virtual void SetVariable(Dictionary<string, string> variables, string key, string value)
    {
      variables[key] = value;
      if (value.Length < 500) Logger.Debug("ScriptableScraperProvider: Assigned variable: {0} = {1}", key, value);
    }

    protected virtual void RemoveVariable(Dictionary<string, string> variables, string key)
    {
      variables.Remove(key);
      RemoveArrayValues(variables, key);
      Logger.Debug("ScriptableScraperProvider: Removed variable: {0}", key);
    }

    private void RemoveArrayValues(Dictionary<string, string> variables, string key)
    {
      int count = 0;
      while (variables.ContainsKey(key + "[" + count + "]"))
      {
        RemoveVariable(variables, key + "[" + count + "]");
        count++;
      }
    }

    protected bool LoadChildren()
    {
      bool success = true;

      children.Clear();
      foreach (XmlNode currXmlNode in xmlNode.ChildNodes)
      {
        ScraperNode newScraperNode = ScraperNode.Load(currXmlNode, Context);
        if (newScraperNode != null && newScraperNode.LoadSuccess)
          children.Add(newScraperNode);

        if (newScraperNode != null && !newScraperNode.LoadSuccess)
          success = false;
      }

      return success;
    }

    protected void ExecuteChildren(Dictionary<string, string> variables)
    {
      foreach (ScraperNode currChild in children)
        currChild.Execute(variables);
    }

    // scans the given string and replaces any existing variables with their value
    protected string ParseString(Dictionary<string, string> variables, string input)
    {
      StringBuilder output = new StringBuilder(input);
      int offset = 0;

      Regex variablePattern = new Regex(@"\${([^:{}]+)(?::([^}\(]+))?(?:\(([^\)]+)\))?}");
      MatchCollection matches = variablePattern.Matches(input);
      foreach (Match currMatch in matches)
      {
        string varName = "";
        string modifier = string.Empty;
        string value = string.Empty;
        string options = string.Empty;

        // get rid of the escaped variable string
        output.Remove(currMatch.Index + offset, currMatch.Length);

        // grab details for this parse
        varName = currMatch.Groups[1].Value;
        variables.TryGetValue(varName, out value);
        if (currMatch.Groups.Count >= 3)
          modifier = currMatch.Groups[2].Value.ToLower();
        if (currMatch.Groups.Count >= 4)
          options = currMatch.Groups[3].Value;

        // if there is no variable for what was passed in we are done
        if (value == string.Empty || value == null)
        {
          offset -= currMatch.Length;
          continue;
        }

        // handle any modifiers
        if (!string.IsNullOrWhiteSpace(modifier))
        {
          IValueModifier handler = Load(modifier);
          if (handler != null)
          {
            value = handler.Parse(this.Context, value, options);
          }
        }

        output.Insert(currMatch.Index + offset, value);
        offset = offset - currMatch.Length + value.Length;
      }

      // if we did some replacements search again to check for embedded variables
      if (matches.Count > 0)
        return ParseString(variables, output.ToString());
      else return output.ToString();
    }

    #endregion

    #region Static Methods

    static ScraperNode()
    {
      TypeCache = new Dictionary<string, Type>();
    }

    /// <summary>
    /// Loads the specified modifier by name.
    /// </summary>
    /// <param name="modifier">The modifier.</param>
    /// <returns>the modifier instance</returns>
    public static IValueModifier Load(string modifier)
    {
      // TODO: cache one instance for every modifier (no need to recreate instance)

      Type modifierType = null;
      string modifierTypeName = modifier.ToLower();
      string modifierTypeKey = "modifier:" + modifierTypeName;

      // try to grab the type from our dictionary
      if (TypeCache.ContainsKey(modifierTypeKey))
        modifierType = TypeCache[modifierTypeKey];

      // if it's not there, search the assembly for the type
      else
      {
        modifierType = (Type)Assembly.GetExecutingAssembly().GetTypes()
                        .Where(t => typeof(IValueModifier).IsAssignableFrom(t)
                            && t.GetCustomAttributes(true).Any(a => a.GetType() == typeof(ValueModifierAttribute)
                                && modifierTypeName.Equals(((ValueModifierAttribute)a).Name)))
                        .SingleOrDefault();

        // store our type and put it in our dictionary so we don't have to
        // look it up again
        if (modifierType != null)
        {
          TypeCache[modifierTypeKey] = modifierType;
        }
      }

      if (modifierType == null)
      {
        Logger.Error("ScriptableScraperProvider: Unsupported modifier type: {0}", modifierTypeName);
        return null;
      }

      try
      {
        // create new ValueModifier
        return (IValueModifier)Activator.CreateInstance(modifierType);
      }
      catch (Exception e)
      {
        if (e.GetType() == typeof(ThreadAbortException))
          throw e;

        Logger.Error("ScriptableScraperProvider: Error instantiating Modifier based on: {0}", modifier, e);
        return null;
      }
    }

    public static ScraperNode Load(XmlNode xmlNode, ScriptableScraper context)
    {
      if (xmlNode == null || xmlNode.NodeType == XmlNodeType.Comment || xmlNode.NodeType == XmlNodeType.CDATA)
        return null;

      Type nodeType = null;
      string nodeTypeName = xmlNode.Name;
      string nodeTypeKey = "node:" + nodeTypeName;

      // try to grab the type from our dictionary
      if (TypeCache.ContainsKey(nodeTypeKey))
        nodeType = TypeCache[nodeTypeKey];

      // if it's not there, search the assembly for the type
      else
      {
        Type[] typeList = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type currType in typeList)
          foreach (Attribute currAttr in currType.GetCustomAttributes(true))
            if (currAttr.GetType() == typeof(ScraperNodeAttribute) &&
                nodeTypeName.Equals(((ScraperNodeAttribute)currAttr).NodeName))
            {

              // store our type and put it in our dictionary so we don't have to
              // look it up again
              TypeCache[nodeTypeKey] = currType;
              nodeType = currType;
              break;
            }
      }

      // if we couldn't find anything log the unhandled node and exit
      if (nodeType == null)
      {
        Logger.Error("ScriptableScraperProvider: Unsupported node type: {0}", xmlNode.OuterXml);
        return null;
      }


      // try to create a new scraper node object
      try
      {
        ConstructorInfo constructor = nodeType.GetConstructor(new Type[] { typeof(XmlNode), typeof(ScriptableScraper) });
        ScraperNode newNode = (ScraperNode)constructor.Invoke(new object[] { xmlNode, context });
        return newNode;
      }
      catch (Exception e)
      {
        if (e.GetType() == typeof(ThreadAbortException))
          throw e;

        Logger.Error("ScriptableScraperProvider: Error instantiating ScraperNode based on: {0}", xmlNode.OuterXml, e);
        return null;
      }
    }

    #endregion
  }

  public interface IValueModifier
  {
    /// <summary>
    /// Parses the specified value.
    /// </summary>
    /// <param name="context">The context of the modifier.</param>
    /// <param name="value">The value that has to be parsed.</param>
    /// <param name="options">The options specified for the modifier.</param>
    /// <returns>the modified value</returns>
    string Parse(ScriptableScraper context, string value, string options);
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class ScraperNodeAttribute : Attribute
  {
    public string NodeName { get; }
    public bool LoadNameAttribute { get; set; } = true;

    public ScraperNodeAttribute(string nodeName)
    {
      NodeName = nodeName;
    }
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class ValueModifierAttribute : Attribute
  {
    public string Name { get; }
    public bool LoadNameAttribute { get; set; } = true;

    public ValueModifierAttribute(string name)
    {
      Name = name;
    }
  }
}
