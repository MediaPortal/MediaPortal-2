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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace MediaPortal.Extensions.MetadataExtractors.ScriptableMetadataExtractor.Data.Nodes
{
  [ScraperNode("if", LoadNameAttribute = false)]
  public class IfNode : ScraperNode
  {
    public string Test { get; protected set; }

    public IfNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "test":
            Test = attr.Value;
            break;
        }
      }

      // Validate TEST attribute
      if (Test == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing TEST attribute on: {0}", xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing if: {0}", xmlNode.Attributes.GetNamedItem("test").InnerText);

      // try to split the test on the operator, quit if we fail
      Regex splitter = new Regex("\\s*(.*?)\\s*(>=|<=|!=|=|<|>)\\s*(.*)$");
      Match match = splitter.Match(Test);
      if (match.Groups.Count != 4)
      {
        Logger.Error("Error parsing test for: " + Test);
        return;
      }

      string left = match.Groups[1].Value;
      string op = match.Groups[2].Value;
      string right = match.Groups[3].Value;

      left = ParseString(variables, left);
      right = ParseString(variables, right);

      Logger.Debug("ScriptableScraperProvider: if node left value: {0}     right value: {1}", left.Length < 500 ? left : "[not logged due to size]", right.Length < 500 ? right : "[not logged due to size]");

      float leftNum = 0;
      float rightNum = 0;

      bool numeric = float.TryParse(left, out leftNum);
      numeric = numeric && float.TryParse(right, out rightNum);

      // try to process our test
      bool testPassed;
      if (op == ">=")
      {
        if (numeric) testPassed = leftNum >= rightNum;
        else testPassed = left.CompareTo(right) >= 0;
      }
      else if (op == "<=")
      {
        if (numeric) testPassed = leftNum <= rightNum;
        else testPassed = left.CompareTo(right) <= 0;
      }
      else if (op == "!=")
      {
        if (numeric) testPassed = leftNum != rightNum;
        else testPassed = left.CompareTo(right) != 0;
      }
      else if (op == "=")
      {
        if (numeric) testPassed = leftNum == rightNum;
        else testPassed = left.CompareTo(right) == 0;
      }
      else if (op == "<")
      {
        if (numeric) testPassed = leftNum < rightNum;
        else testPassed = left.CompareTo(right) < 0;
      }
      else if (op == ">")
      {
        if (numeric) testPassed = leftNum > rightNum;
        else testPassed = left.CompareTo(right) > 0;
      }
      else
      {
        Logger.Error("ScriptableScraperProvider: Unrecognized operator: {0}", op);
        return;
      }

      // if the test passed execute the child nodes
      if (testPassed)
        ExecuteChildren(variables);
    }
  }
}
