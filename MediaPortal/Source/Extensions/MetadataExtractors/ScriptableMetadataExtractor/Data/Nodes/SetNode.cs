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
  [ScraperNode("set")]
  public class SetNode : ScraperNode
  {
    #region Properties

    public string Value { get; protected set; }

    #endregion

    #region Methods

    public SetNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
      Logger.Debug("ScriptableScraperProvider: Executing set: " + xmlNode.OuterXml);

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "value":
            Value = attr.Value;
            break;
        }
      }

      // get the inner value
      string innerValue = xmlNode.InnerText.Trim();

      // Validate VALUE attribute
      if (Value == null)
      {
        Value = innerValue;
        if (innerValue.Equals(String.Empty))
        {
          Logger.Error("ScriptableScraperProvider: Missing VALUE attribute on: " + xmlNode.OuterXml);
          LoadSuccess = false;
          return;
        }
      }
      else if (!innerValue.Equals(String.Empty))
      {
        Logger.Error("ScriptableScraperProvider: Ambiguous assignment on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

    }

    public override void Execute(Dictionary<string, string> variables)
    {
      SetVariable(variables, ParseString(variables, Name), ParseString(variables, Value));
    }

    #endregion
  }
}
