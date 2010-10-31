#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
  [XmlRoot("Language")]
  public class StringFile
  {
    #region Variables

    [XmlAttribute("Name")]
    public string _languageName;

    [XmlElement("Section")]
    public List<StringSection> _sections;

    #endregion

    #region Public Members

    [XmlIgnore]
    public string LanguageName
    {
      get { return _languageName; }
    }

    [XmlIgnore]
    public ICollection<StringSection> Sections
    {
      get { return _sections; }
    }

    public bool IsSection(string sectionName)
    {
      if (_sections != null)
      {
        foreach (StringSection section in _sections)
        {
          if (section.SectionName == sectionName)
            return true;
        }
      }
      return false;
    }

    public void AddSection(StringSection section)
    {
      if (_sections == null)
        _sections = new List<StringSection>();

      _sections.Add(section);
    }

    #endregion
  }
}
