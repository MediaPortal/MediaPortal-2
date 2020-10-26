#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("sleep", LoadNameAttribute = false)]
  public class SleepNode : ScraperNode
  {
    #region Properties

    public int Length { get; protected set; }

    #endregion

    #region Methods

    public SleepNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      Logger.Debug("ScriptableScraperProvider: Executing set: {0}", xmlNode.OuterXml);

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "length":
            if (int.TryParse(attr.Value, out var length))
              Length = length;
            else
              Length = 100;
            break;
        }
      }

      // get the inner value
      string innerValue = xmlNode.InnerText.Trim();

      // Validate length attribute
      if (Length <= 0)
      {
        Logger.Error("ScriptableScraperProvider: The LENGTH attribute must be greater than 0: {0}", xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Thread.Sleep(Length);
    }

    #endregion
  }
}
