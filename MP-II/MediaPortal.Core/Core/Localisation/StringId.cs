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

#region Copyright (C) 2005-2008 Team MediaPortal

/* 
 *	Copyright (C) 2005-2008 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Text.RegularExpressions;

//using SkinEngine.Logging;

namespace MediaPortal.Core.Localisation
{
  /// <summary>
  /// Generates string section and id from combo string 
  /// </summary>
  public class StringId
  {
    private string _section;
    private string _name;
    private string _localised;

    public StringId()
    {
      _section = "system";
      _name = "invalid";
    }

    public StringId(string section, string name)
    {
      _section = section;
      _name = name;

      ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);
    }

    public StringId(string skinLabel)
    {
      // Parse string example [section.name]
      if (IsString(skinLabel))
      {
        int pos = skinLabel.IndexOf('.');
        _section = skinLabel.Substring(1, pos - 1).ToLower();
        _name = skinLabel.Substring(pos + 1, skinLabel.Length - pos - 2).ToLower();

        ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);
      }
      else
      {
        _localised = skinLabel;
      }
    }

    public void Dispose()
    {
      ServiceScope.Get<ILocalisation>().LanguageChange -= LangageChange;
    }

    public string Section
    {
      get { return _section; }
    }

    public string Name
    {
      get { return _name; }
    }

    private void LangageChange(object o)
    {
      _localised = null;
    }

    public override string ToString()
    {
      if (_localised == null)
        _localised = ServiceScope.Get<ILocalisation>().ToString(this);

      if (_localised == null)
        return _section + "." + _name;
      else
        return _localised;
    }

    public static bool IsString(string testString)
    {
      if (testString != null && testString.StartsWith("[") && testString.EndsWith("]") && testString.Contains("."))
        return true;

      return false;
    }
  }
}
