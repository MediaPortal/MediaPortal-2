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

using System;
using System.Text.RegularExpressions;

//using SkinEngine.Logging;

namespace MediaPortal.Core.Localisation
{
  /// <summary>
  /// String descriptor for text strings to be displayed in the GUI. Strings referenced
  /// by this descriptor can be localized by the localization API.
  /// </summary>
  /// <remarks>
  /// String descriptors of this class hold a section name and a name of the to-be-localized
  /// string. These values are used to lookup the localized string in the language resource.
  /// <see cref="MediaPortal.Core.Localisation.ILocalization"/>
  /// </remarks>
  public class StringId
  {
    private string _section;
    private string _name;
    private string _localised;

    /// <summary>
    /// Creates a new invalid string descriptor.
    /// </summary>
    public StringId()
    {
      _section = "system";
      _name = "invalid";
    }

    /// <summary>
    /// Creates a new string descriptor with the specified data.
    /// </summary>
    /// <param name="section">The section in the language resource
    /// where the localized string will be searched.</param>
    /// <param name="name">The name of the string in the language resource.</param>
    public StringId(string section, string name)
    {
      _section = section;
      _name = name;

      ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);
    }

    /// <summary>
    /// Creates a new string descriptor given a label describing the string. This label may come
    /// from a skin file, for example.
    /// </summary>
    /// <param name="skinLabel">A label describing the localized string. This label has to be in the
    /// form <c>[section.name]</c>.</param>
    public StringId(string label)
    {
      // Parse string example [section.name]
      if (IsResourceString(label))
      {
        int pos = label.IndexOf('.');
        _section = label.Substring(1, pos - 1).ToLower();
        _name = label.Substring(pos + 1, label.Length - pos - 2).ToLower();

        ServiceScope.Get<ILocalisation>().LanguageChange += new LanguageChangeHandler(LangageChange);
      }
      else
      {
        // Should we raise an exception here?
        _section = "system";
        _name = label;
      }
    }

    public void Dispose()
    {
      ServiceScope.Get<ILocalisation>().LanguageChange -= LangageChange;
    }

    /// <summary>
    /// The section name where the localized string will be searched in the language resource.
    /// </summary>
    public string Section
    {
      get { return _section; }
    }

    /// <summary>
    /// The name of the localized string in the language resource.
    /// </summary>
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
        return "["+_section + "." + _name+"]";
      else
        return _localised;
    }

    /// <summary>
    /// Tests if the given string is of form <c>[section.name]</c> and hence can be looked up
    /// in a language resource.
    /// </summary>
    /// <param name="testString">The label to be tested.</param>
    /// <returns>true, if the given label is in the correct form to describe a language resource
    /// string, else false</returns>
    public static bool IsResourceString(string label)
    {
      if (label != null && label.StartsWith("[") && label.EndsWith("]") && label.Contains("."))
        return true;

      return false;
    }
  }
}
