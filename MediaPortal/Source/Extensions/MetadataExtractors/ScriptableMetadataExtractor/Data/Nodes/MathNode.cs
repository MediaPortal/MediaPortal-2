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
using System.Globalization;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  public abstract class MathNode : ScraperNode
  {
    public enum ResultTypeEnum
    {
      INT,
      FLOAT
    }

    public string Value1 { get; protected set; }
    public string Value2 { get; protected set; }
    public ResultTypeEnum ResultType { get; protected set; }

    public MathNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      // Load attributes
      string resultTypeStr = null;
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "value1":
            Value1 = attr.Value;
            break;
          case "value2":
            Value2 = attr.Value;
            break;
          case "result_type":
            resultTypeStr = attr.Value;
            break;
        }
      }

      // Validate VALUE1 attribute
      if (Value1 == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing VALUE1 attribute on: {0}", xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

      // Validate VALUE2 attribute
      if (Value2 == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing VALUE2 attribute on: {0}", xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }

      // Validate RESULT_TYPE attribute
      if (resultTypeStr != null && resultTypeStr.ToUpper().Equals("FLOAT"))
        ResultType = ResultTypeEnum.FLOAT;
      else
        ResultType = ResultTypeEnum.INT;

    }
  }

  [ScraperNode("add")]
  public class AddNode : MathNode
  {
    public AddNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing add: {0}", xmlNode.OuterXml);

      string parsedValue1 = ParseString(variables, Value1);
      string parsedValue2 = ParseString(variables, Value2);

      try
      {
        float val1 = float.Parse(parsedValue1, CultureInfo.InvariantCulture);
        float val2 = float.Parse(parsedValue2, CultureInfo.InvariantCulture);

        if (ResultType == ResultTypeEnum.INT)
          SetVariable(variables, ParseString(variables, Name), ((int)val1 + val2).ToString());
        if (ResultType == ResultTypeEnum.FLOAT)
          SetVariable(variables, ParseString(variables, Name), (val1 + val2).ToString());
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing numbers: {0}", xmlNode.OuterXml, e);
      }
    }
  }

  [ScraperNode("subtract")]
  public class SubtractNode : MathNode
  {
    public SubtractNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing subtract: {0}", xmlNode.OuterXml);

      string parsedValue1 = ParseString(variables, Value1);
      string parsedValue2 = ParseString(variables, Value2);

      try
      {
        float val1 = float.Parse(parsedValue1, new CultureInfo("en-US", false));
        float val2 = float.Parse(parsedValue2, new CultureInfo("en-US", false));

        if (ResultType == ResultTypeEnum.INT)
          SetVariable(variables, ParseString(variables, Name), ((int)val1 - val2).ToString());
        if (ResultType == ResultTypeEnum.FLOAT)
          SetVariable(variables, ParseString(variables, Name), (val1 - val2).ToString());
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing numbers: {0}", xmlNode.OuterXml, e);
      }
    }
  }

  [ScraperNode("multiply")]
  public class MultiplyNode : MathNode
  {
    public MultiplyNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing multiply: {0}", xmlNode.OuterXml);
      string parsedValue1 = ParseString(variables, Value1);
      string parsedValue2 = ParseString(variables, Value2);

      try
      {
        float val1 = float.Parse(parsedValue1, new CultureInfo("en-US", false));
        float val2 = float.Parse(parsedValue2, new CultureInfo("en-US", false));

        if (ResultType == ResultTypeEnum.INT)
          SetVariable(variables, ParseString(variables, Name), ((int)val1 * val2).ToString());
        if (ResultType == ResultTypeEnum.FLOAT)
          SetVariable(variables, ParseString(variables, Name), (val1 * val2).ToString());
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing numbers: {0}", xmlNode.OuterXml, e);
      }
    }
  }

  [ScraperNode("divide")]
  public class DivideNode : MathNode
  {
    public DivideNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing divide: {0}", xmlNode.OuterXml);
      string parsedValue1 = ParseString(variables, Value1);
      string parsedValue2 = ParseString(variables, Value2);

      try
      {
        float val1 = float.Parse(parsedValue1, new CultureInfo("en-US", false));
        float val2 = float.Parse(parsedValue2, new CultureInfo("en-US", false));

        if (ResultType == ResultTypeEnum.INT)
          SetVariable(variables, ParseString(variables, Name), ((int)(val1 / val2)).ToString());
        if (ResultType == ResultTypeEnum.FLOAT)
          SetVariable(variables, ParseString(variables, Name), (val1 / val2).ToString());
      }
      catch (Exception e)
      {
        Logger.Error("ScriptableScraperProvider: Error parsing numbers: {0}", xmlNode.OuterXml, e);
      }
    }
  }
}
