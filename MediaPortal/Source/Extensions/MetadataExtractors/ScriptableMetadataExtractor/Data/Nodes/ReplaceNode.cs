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
using System.Text.RegularExpressions;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("replace")]
  public class ReplaceNode : ScraperNode
  {
    #region Properties

    public string Input { get; protected set; }
    public string Pattern { get; protected set; }
    public string With { get; protected set; }

    #endregion

    #region Methods
    public ReplaceNode(XmlNode xmlNode, ScriptableScraper context)
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
          case "pattern":
            Pattern = attr.Value;
            break;
          case "with":
            With = attr.Value;
            break;
        }
      }

      // Validate INPUT attribute
      if (Input == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing INPUT attribute on: {0}", xmlNode.OuterXml);
        return;
      }

      // Validate PATTERN attribute
      if (Pattern == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing PATTERN attribute on: {0}", xmlNode.OuterXml);
        return;
      }

      // Validate WITH attribute
      if (With == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing WITH attribute on: {0}", xmlNode.OuterXml);
        return;
      }

      LoadSuccess = true;
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing replace: {0}", xmlNode.OuterXml);
      string output = string.Empty;
      try
      {
        output = Regex.Replace(ParseString(variables, Input), ParseString(variables, Pattern), ParseString(variables, With));
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: An error occurred while executing replace.", e);
        return;
      }
      SetVariable(variables, ParseString(variables, Name), output);
    }

    #endregion Methods
  }
}
