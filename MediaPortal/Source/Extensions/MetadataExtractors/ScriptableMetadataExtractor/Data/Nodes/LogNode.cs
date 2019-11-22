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
  [ScraperNode("log", LoadNameAttribute = false)]
  public class LogNode : ScraperNode
  {
    public enum LoggingLevel
    {
      Error,
      Warn,
      Info,
      Debug
    }

    #region Properties

    public LoggingLevel LogLevel { get; protected set; }
    public string Message { get; protected set; }

    #endregion

    public LogNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
      try
      {
        if (Enum.TryParse<LoggingLevel>(xmlNode.Attributes["LogLevel"].Value, out var lvl))
          LogLevel = lvl;
        else if (Enum.TryParse<LoggingLevel>(xmlNode.Attributes["log_level"].Value, out var lvl2))
          LogLevel = lvl2;
      }
      catch
      {
        LogLevel = LoggingLevel.Debug;
      }

      try
      {
        Message = xmlNode.Attributes["Message"].Value;
      }
      catch
      {
        try
        {
          Message = xmlNode.Attributes["message"].Value;
        }
        catch (Exception e)
        {
          Logger.Error("ScriptableScraperProvider: Missing MESSAGE attribute on: " + xmlNode.OuterXml, e);
          LoadSuccess = false;
          return;
        }
      }

      LoadSuccess = true;
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      switch (LogLevel)
      {
        case LoggingLevel.Error:
          Logger.Error("ScriptableScraperProvider: " + ParseString(variables, Message));
          break;
        case LoggingLevel.Warn:
          Logger.Warn("ScriptableScraperProvider: " + ParseString(variables, Message));
          break;
        case LoggingLevel.Info:
          Logger.Info("ScriptableScraperProvider: " + ParseString(variables, Message));
          break;
        case LoggingLevel.Debug:
          Logger.Debug("ScriptableScraperProvider: " + ParseString(variables, Message));
          break;
      }
    }
  }
}
