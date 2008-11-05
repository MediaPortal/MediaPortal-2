#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Xml;

namespace MediaPortal.Utilities.Scraper
{
  public class ScraperSetting
  {
    private string _label;
    private string _type;
    private string _id;
    private string _default;
    private string _value;

    public string Label
    {
      get
      {
        return _label;
      }
      set
      {
        _label = value;
      }
    }

    public string Type
    {
      get
      {
        return _type;
      }
      set
      {
        _type = value;
      }
    }

    public string Id
    {
      get
      {
        return _id;
      }
      set
      {
        _id = value;
      }
    }

    public string Default
    {
      get
      {
        return _default;
      }
      set
      {
        _default = value;
      }
    }

    public string Value
    {
      get
      {
        return _value;
      }
      set
      {
        _value = value;
      }
    }

    public ScraperSetting(XmlNode node)
    {
      if (node.Attributes["label"] != null)
        Label = node.Attributes["label"].Value;
      if (node.Attributes["type"] != null)
        Type = node.Attributes["type"].Value;
      if (node.Attributes["id"] != null)
        Id = node.Attributes["id"].Value;
      if (node.Attributes["default"] != null)
      {
        Default = node.Attributes["default"].Value;
        Value = Default;
      }
    }

    public ScraperSetting(string label, string type, string id, string defaul)
    {
      Label = label;
      Type = type;
      Id = id;
      Default = defaul;
      Value = Default;
    }

    public bool GetValueAsBool()
    {
      if (Value == "true")
        return true;
      return false;
    }
  }
}
