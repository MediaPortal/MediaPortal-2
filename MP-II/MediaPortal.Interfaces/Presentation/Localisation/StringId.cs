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

using MediaPortal.Core;

namespace MediaPortal.Presentation.Localisation
{
  /// <summary>
  /// String descriptor for text strings to be displayed in the GUI. Strings referenced
  /// by this descriptor can be localized by the localization API.
  /// </summary>
  /// <remarks>
  /// String descriptors of this class hold a section name and a name of the to-be-localized
  /// string. These values are used to lookup the localized string in the language resource.
  /// <see cref="ILocalisation"/>
  /// </remarks>
  public class StringId
  {
    #region Protected fields

    protected readonly string _section;
    protected readonly string _name;
    protected string _localised;

    #endregion

    /// <summary>
    /// Creates a new invalid string descriptor.
    /// </summary>
    public StringId()
    {
      _section = "system";
      _name = "invalid";
    }

    /// <summary>
    /// Creates a new string descriptor for the specified section and name.
    /// </summary>
    /// <param name="section">The section in the language resource
    /// where the localized string will be searched.</param>
    /// <param name="name">The name of the string in the specified section.</param>
    public StringId(string section, string name)
    {
      _section = section;
      _name = name;

      ServiceScope.Get<ILocalisation>().LanguageChange += OnLangageChange;
    }

    /// <summary>
    /// Creates a new string descriptor given a label describing the string. This label may come
    /// from a skin file, for example.
    /// </summary>
    /// <param name="label">A label describing the localized string. This label has to be in the
    /// form <c>[section.name]</c>.</param>
    public StringId(string label)
    {
      if (IsResourceString(label))
      { // Parse string if it has the form [section.name]
        ParseResourceString(label, out _section, out _name);
        ServiceScope.Get<ILocalisation>().LanguageChange += OnLangageChange;
      }
      else
      { // No resource string - use plain string
        _section = "plain"; // Dummy section & name
        _name = label;
        _localised = label;
      }
    }

    public void Dispose()
    {
      ServiceScope.Get<ILocalisation>().LanguageChange -= OnLangageChange;
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

    public string Label
    {
      get { return "[" + _section + "." + _name + "]"; }
    }

    private void OnLangageChange(object o)
    {
      _localised = null;
    }

    public override string ToString()
    {
      if (_localised == null)
        _localised = ServiceScope.Get<ILocalisation>().ToString(this);

      if (_localised == null)
        return Label;
      else
        return _localised;
    }

    /// <summary>
    /// Parses the section and the name part of a given <see cref="StringId"/> label.
    /// </summary>
    /// <param name="label">Label to be parsed. The label has to be in the form
    /// <code>[section.name]</code>.</param>
    /// <param name="section">Parsed section.</param>
    /// <param name="name">Parsed name.</param>
    protected static void ParseResourceString(string label, out string section, out string name)
    {
      int pos = label.IndexOf('.');
      section = label.Substring(1, pos - 1).ToLower();
      name = label.Substring(pos + 1, label.Length - pos - 2).ToLower();
    }

    /// <summary>
    /// Tests if the given string is of form <c>[section.name]</c> and hence can be looked up
    /// in a language resource.
    /// </summary>
    /// <param name="label">The label to be tested.</param>
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
