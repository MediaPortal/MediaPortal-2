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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("parse")]
  public class ParseNode : ScraperNode
  {
    #region Properties

    public string Input { get; protected set; }
    public string Pattern { get; protected set; }
    public string Xpath { get; protected set; }

    #endregion

    #region Methods

    public ParseNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "input":
            Input = attr.Value;
            break;
          case "regex":
            Pattern = attr.Value;
            break;
          case "xpath":
            Xpath = attr.Value;
            break;
        }
      }

      // Validate INPUT attribute
      if (Input == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing INPUT attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

      // Validate REGEX/XPATH attribute
      if (Pattern == null && Xpath == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing REGEX or XPATH attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing parse: " + xmlNode.OuterXml);
      // parse variables from the input string
      string parsedInput = ParseString(variables, Input);
      string parsedName = ParseString(variables, Name);

      // do requested parsing
      if (Pattern != null)
        ProcessPattern(variables, parsedInput, parsedName);
      else
        ProcessXpath(variables, parsedInput, parsedName);
    }

    // Parse input using a regular expression
    private void ProcessPattern(Dictionary<string, string> variables, string parsedInput, string parsedName)
    {
      string parsedPattern = ParseString(variables, Pattern);

      Logger.Debug("ScriptableScraperProvider: Name: " + parsedName + " ||| pattern: " + parsedPattern + " ||| input: " + (parsedInput != null && parsedInput.Length < 500 ? parsedInput : "[not logged due to size]"));

      // try to find matches via regex pattern
      MatchCollection matches;
      try
      {
        Regex regEx = new Regex(parsedPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        matches = regEx.Matches(parsedInput);
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Regex expression failed!", e);
        return;
      }

      SetVariable(variables, parsedName + ".count", matches.Count.ToString());

      if (matches.Count == 0)
      {
        Logger.Debug("ScriptableScraperProvider: Parse node returned no results... " + xmlNode.OuterXml);
        return;
      }

      SetVariable(variables, parsedName, matches[0].Value);

      // write matches and groups to variables
      int matchNum = 0;
      foreach (Match currMatch in matches)
      {
        // store the match itself
        string matchName = parsedName + "[" + matchNum + "]";
        SetVariable(variables, matchName, currMatch.Value);

        // store the groups in the match
        for (int i = 1; i < currMatch.Groups.Count; i++)
          SetVariable(variables, matchName + "[" + (i - 1) + "]", currMatch.Groups[i].Value);

        matchNum++;
      }
    }

    // Parse input using an xpath query
    private void ProcessXpath(Dictionary<string, string> variables, string parsedInput, string parsedName)
    {
      string query = ParseString(variables, Xpath);

      try
      {
        XPathDocument xml = new XPathDocument(new StringReader(parsedInput));
        XPathNavigator navigator = xml.CreateNavigator();
        XPathNodeIterator nodes = navigator.Select(query);

        SetVariable(variables, parsedName + ".count", nodes.Count.ToString());

        while (nodes.MoveNext())
        {
          XPathNavigator node = nodes.Current;
          string varName = parsedName + "[" + (nodes.CurrentPosition - 1).ToString() + "]";
          ParseChildNode(variables, varName, node, true);
        }
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Scraper Script XPATH parsing failed", e);
      }
    }

    private void ParseChildNode(Dictionary<string, string> variables, string name, XPathNavigator node, bool recursive)
    {
      XPathNodeIterator childNodes = node.SelectChildren(XPathNodeType.Element);
      if (childNodes.Count > 0)
      {
        // Create nodeset variable
        SetVariable(variables, name, node.OuterXml);

        // Parse Children If Required
        // TODO: if multiple children with the same name exist only the last
        // one will have a variable. It should be clear that in this case
        // the scripter should parse the OuterXml value 
        if (recursive)
        {
          while (childNodes.MoveNext())
          {
            XPathNavigator child = childNodes.Current;
            string varName = name + "." + child.Name;
            ParseChildNode(variables, varName, child, false);
          }
        }
      }
      else
      {
        // Create node variable
        SetVariable(variables, name, node.Value);
      }

      // create attribute variables
      if (node.HasAttributes && recursive)
      {
        XPathNavigator attrib = node.Clone();
        attrib.MoveToFirstAttribute();
        SetVariable(variables, name + ".@" + attrib.Name, attrib.Value);
        while (attrib.MoveToNextAttribute())
        {
          SetVariable(variables, name + ".@" + attrib.Name, attrib.Value);
        }
      }
    }

    #endregion
  }
}
