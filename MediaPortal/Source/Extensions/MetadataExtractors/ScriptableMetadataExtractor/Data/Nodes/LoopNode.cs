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
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("loop")]
  public class LoopNode : ScraperNode
  {
    #region Properties

    public string LoopingVariable { get; protected set; }
    public int Limit { get; protected set; } = 10;

    #endregion

    #region Methods

    public LoopNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
      Logger.Debug("ScriptableScraperProvider: Executing loop: " + xmlNode.OuterXml);

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "on":
            LoopingVariable = attr.Value;
            break;
          case "limit":
            try
            {
              Limit = int.Parse(attr.Value);
            }
            catch (Exception e)
            {
              Logger.Error("ScriptableScraperProvider: Invalid value for LIMIT attribute on: " + xmlNode.OuterXml, e);
            }
            break;
        }
      }

      // Validate ON attribute
      if (LoopingVariable == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing ON attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      string parsedName = ParseString(variables, Name);

      int count = 0;
      while (variables.ContainsKey(LoopingVariable + "[" + count + "]") && count < Limit)
      {
        string oldName = LoopingVariable + "[" + count + "]";
        SetVariable(variables, parsedName, ParseString(variables, "${" + oldName + "}"));
        SetVariable(variables, "count", count.ToString());
        TranscribeArrayValues(variables, parsedName, oldName);

        ExecuteChildren(variables);

        RemoveVariable(variables, parsedName);
        RemoveVariable(variables, "count");
        count++;
      }
    }

    // if the variable we are looping on itself is an array, then propagate 
    // the array elements down as well
    protected void TranscribeArrayValues(Dictionary<string, string> variables, string baseName, string oldName)
    {
      int count = 0;
      while (variables.ContainsKey(oldName + "[" + count + "]"))
      {
        SetVariable(variables, baseName + "[" + count + "]", ParseString(variables, "${" + oldName + "[" + count + "]}"));
        TranscribeArrayValues(variables, baseName + "[" + count + "]", oldName + "[" + count + "]");
        count++;
      }
    }

    #endregion
  }
}
