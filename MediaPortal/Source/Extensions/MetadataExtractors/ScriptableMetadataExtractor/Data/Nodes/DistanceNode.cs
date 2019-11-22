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

using MediaPortal.Utilities;
using System.Collections.Generic;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("distance")]
  public class DistanceNode : ScraperNode
  {
    public string String1 { get; protected set; }
    public string String2 { get; protected set; }

    public DistanceNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "string1":
            String1 = attr.Value;
            break;
          case "string2":
            String2 = attr.Value;
            break;
        }
      }


      // Validate STRING1 attribute
      if (String1 == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing STRING1 attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

      // Validate STRING2 attribute
      if (String2 == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing STRING2 attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing distance: " + xmlNode.OuterXml);

      string parsedString1 = ParseString(variables, String1);
      string parsedString2 = ParseString(variables, String2);
      Logger.Debug("ScriptableScraperProvider: Executing distance: " + parsedString1 + " vs. " + parsedString2);

      int distance = StringUtils.GetLevenshteinDistance(parsedString1, parsedString2);

      SetVariable(variables, ParseString(variables, Name), distance.ToString());
    }

  }
}
