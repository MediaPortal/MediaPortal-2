#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Xml.Serialization;

namespace MediaPortal.Utilities.Localization.StringsFile
{
  public class StringSection
  {
    #region Variables

    [XmlAttribute("Name")]
    public string _name;

    [XmlElement("String")]
    public List<StringLocalized> _localizedStrings;

    #endregion

    #region Public Members

    [XmlIgnore]
    public string SectionName
    {
      get { return _name; }
    }

    [XmlIgnore]
    public ICollection<StringLocalized> LocalizedStrings
    {
      get { return _localizedStrings; }
    }

    public bool IsString(string stringName)
    {
      if (_localizedStrings != null)
      {
        foreach (StringLocalized str in _localizedStrings)
        {
          if (str.StringName == stringName)
            return true;
        }
      }
      return false;
    }

    public void AddString(StringLocalized str)
    {
      if (_localizedStrings == null)
        _localizedStrings = new List<StringLocalized>();

      _localizedStrings.Add(str);
    }

    #endregion
  }
}
