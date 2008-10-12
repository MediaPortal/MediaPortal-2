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
using MediaPortal.Core;

namespace MediaPortal.Presentation.Localisation
{
  /// <summary>
  /// String descriptor for localized text strings to be displayed in the GUI.
  /// </summary>
  /// <remarks>
  /// String descriptors of this class hold a section name and a name of the to-be-localized
  /// string. These values are used to lookup the localized string in the language resource.
  /// <see cref="MediaPortal.Presentation.Localisation.ILocalisation"/>
  /// </remarks>
  public class StringId : IComparable<StringId>, IEquatable<StringId>
  {
    public const string INVALID_LABEL = "[system.invalid]";

    #region Protected fields

    /// <summary>
    /// The section in the language resource where the localized string will be searched.
    /// </summary>
    protected string _section;
    
    /// <summary>
    /// The name of the string in the language resource.
    /// </summary>
    protected string _name;

    /// <summary>
    /// Lazy initialized cache for the localised string.
    /// </summary>
    protected string _localised;

    #endregion

    #region Public properties

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

    /// <summary>
    /// Full link, formatted as "[section.name]".
    /// </summary>
    public string Label
    {
      get { return "[" + _section + "." + _name + "]"; }
    }

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes a new invalid string descriptor. The invalid string descriptor uses
    /// <see cref="INVALID_LABEL"/> as label.
    /// </summary>
    public StringId() : this(INVALID_LABEL) { }

    /// <summary>
    /// Initializes a new string descriptor with the specified data.
    /// </summary>
    /// <param name="section">The section in the language resource
    /// where the localized string will be searched.</param>
    /// <param name="name">The name of the string in the language resource.</param>
    public StringId(string section, string name)
    {
      _section = section;
      _name = name;

      ServiceScope.Get<ILocalisation>().LanguageChange += LanguageChange;
    }

    /// <summary>
    /// Initializes a new string descriptor given a label describing the string. This label may come
    /// from a skin file, for example.
    /// </summary>
    /// <param name="label">A label describing the localized string. This label has to be in the
    /// form <c>[section.name]</c>.</param>
    public StringId(string label)
    {
      // Parse string example [section.name]
      if (IsResourceString(label))
      {
        int pos = label.IndexOf('.');
        _section = label.Substring(1, pos - 1).ToLower();
        _name = label.Substring(pos + 1, label.Length - pos - 2).ToLower();

        ServiceScope.Get<ILocalisation>().LanguageChange += LanguageChange;
      }
      else
      {
        // Should we raise an exception here?
        _section = "system";
        _name = label;
        _localised = label;
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Disposes the instance.
    /// </summary>
    public void Dispose()
    {
      ServiceScope.Get<ILocalisation>().LanguageChange -= LanguageChange;
    }

    /// <summary>
    /// Returns the localised string,
    /// or (if not found) the Label-property.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      if (_localised == null)
        _localised = ServiceScope.Get<ILocalisation>().ToString(this);

      if (_localised == null)
        return Label;
      return _localised;
    }

    #endregion

    #region Private methods

    private void LanguageChange(object o)
    {
      _localised = null;
    }

    #endregion

    #region Static methods

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

    #endregion

    #region IComparable<StringId> implementation

    /// <summary>
    /// Compares the localised strings.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(StringId other)
    {
      return string.Compare(ToString(), other.ToString(), false, ServiceScope.Get<ILocalisation>().CurrentCulture);
    }

    #endregion

    #region IEquatable<StringId> implementation

    public bool Equals(StringId other)
    {
      return Label == other.Label;
    }

    #endregion
  }
}
