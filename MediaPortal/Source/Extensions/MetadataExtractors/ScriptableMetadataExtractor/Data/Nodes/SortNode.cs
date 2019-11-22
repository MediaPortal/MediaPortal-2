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
  [ScraperNode("sort")]
  public class SortNode : ScraperNode
  {
    public enum DirectionType
    {
      ASCENDING,
      DESCENDING
    }

    public DirectionType Direction { get; protected set; }
    public string SortBy { get; protected set; }

    public SortNode(XmlNode xmlNode, ScriptableScraper context)
        : base(xmlNode, context)
    {

      // Load attributes
      foreach (XmlAttribute attr in xmlNode.Attributes)
      {
        switch (attr.Name)
        {
          case "direction":
            string dirStr = attr.Value.ToLower().Trim();
            if (dirStr == "desc" || dirStr == "descending")
              Direction = DirectionType.DESCENDING;
            else if (dirStr == "asc" || dirStr == "ascending")
              Direction = DirectionType.ASCENDING;
            else
              Logger.Error("ScriptableScraperProvider: Invalid sort direction on: " + xmlNode.OuterXml);
            break;
          case "by":
            SortBy = attr.Value;
            break;

        }
      }

      // Validate BY attribute
      if (SortBy == null)
      {
        Logger.Error("ScriptableScraperProvider: Missing BY attribute on: " + xmlNode.OuterXml);
        LoadSuccess = false;
        return;
      }
    }

    public override void Execute(Dictionary<string, string> variables)
    {
      Logger.Debug("ScriptableScraperProvider: Executing sort: " + xmlNode.OuterXml);

      // get our initial parsed settings from the script
      string arrayName = ParseString(variables, Name);
      string parsedSortBy = ParseString(variables, SortBy);

      // build a list of the specified array
      int count = 0;
      List<WeakTypedObject> list = new List<WeakTypedObject>();
      while (variables.ContainsKey(arrayName + "[" + count + "]"))
      {
        WeakTypedObject newObj = new WeakTypedObject(arrayName + "[" + count + "]", variables);
        newObj.SortKey = parsedSortBy;
        list.Add(newObj);
        count++;
      }

      // sort and rewrite the sorted list to the variables dictionary
      list.Sort();
      count = 0;
      foreach (WeakTypedObject currObj in list)
      {
        variables[arrayName + "[" + count + "]"] = currObj.BaseValue;
        foreach (KeyValuePair<string, string> currPair in currObj.Members)
          variables[arrayName + "[" + count + "]" + currPair.Key] = currPair.Value;
        count++;
      }
    }
  }

  public class WeakTypedObject : IComparable
  {
    public string SortKey;
    public Dictionary<string, string> Members;

    public string BaseName
    {
      get { return baseName; }
    }
    protected string baseName;

    public string BaseValue
    {
      get { return baseValue; }
    }
    protected string baseValue;


    public WeakTypedObject(string baseName, Dictionary<string, string> variables)
    {
      this.baseName = baseName;
      loadMembers(variables);
    }

    public string SortValue
    {
      get
      {
        if (Members != null && Members.ContainsKey("." + SortKey))
          return Members["." + SortKey];
        else if (Members != null && Members.ContainsKey(SortKey))
          return Members[SortKey];
        else return null;
      }
    }

    private void loadMembers(Dictionary<string, string> variables)
    {
      Members = new Dictionary<string, string>();
      foreach (KeyValuePair<string, string> currPair in variables)
      {
        if (currPair.Key.StartsWith(BaseName))
        {
          string memberName = currPair.Key.Replace(BaseName, "");
          if (memberName.Length == 0)
          {
            baseValue = currPair.Value;
            continue;
          }
          else if (memberName[0] == '.')
          {
            Members[memberName] = currPair.Value;
          }
          else if (memberName[0] == '[')
          {
            Members[memberName] = currPair.Value;
          }
          else
          {
            continue;
          }
        }
      }
    }

    public int CompareTo(object obj)
    {
      if (SortValue == null)
        return 0;

      if (obj == null || obj.GetType() != typeof(WeakTypedObject))
        return 0;

      // grab the other object and assert it is valid
      WeakTypedObject other = (WeakTypedObject)obj;
      if (other.SortValue == null)
        return 0;

      // try assuming the values are numeric
      try
      {
        float thisFloat = float.Parse(SortValue, CultureInfo.InvariantCulture);
        float otherFloat = float.Parse(other.SortValue, CultureInfo.InvariantCulture);

        return thisFloat == otherFloat ? 0 : thisFloat < otherFloat ? -1 : 1;
      }
      catch
      {
      }

      // otherwise resort to string based sorting
      return SortValue.CompareTo(other.SortValue);
    }
  }
}
