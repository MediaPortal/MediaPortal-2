#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.Common.Localization
{
  /// <summary>
  /// String descriptor for localized text strings to be displayed in the GUI.
  /// </summary>
  /// <remarks>
  /// String descriptors of this class hold a section name and a name of the to-be-localized
  /// string. These values are used to lookup the localized string in the language resource.
  /// <see cref="ILocalization"/>
  /// </remarks>
  public class StringId : IComparable<StringId>, IEquatable<StringId>
  {
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
    /// Lazy initialized cache for the localized string.
    /// </summary>
    protected string _localized;

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
    /// Initializes a new string descriptor with the specified data.
    /// </summary>
    /// <param name="section">The section in the language resource
    /// where the localized string will be searched.</param>
    /// <param name="name">The name of the string in the language resource.</param>
    [Obsolete("Use StringId(string label) instead.")]
    public StringId(string section, string name)
    {
      _section = section;
      _name = name;
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
      if (!ExtractSectionAndName(label, out _section, out _name))
      {
        // Should we raise an exception here?
        _section = "system";
        _name = label;
        _localized = label;
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns the localized string, or (if not found) the Label-property.
    /// </summary>
    /// <returns>Localized string.</returns>
    public override string ToString()
    {
      if (_localized == null)
        _localized = ServiceRegistration.Get<ILocalization>().ToString(string.Format("[{0}.{1}]", _section, _name));

      if (_localized == null)
        return Label;
      return _localized;
    }

    #endregion

    #region Static methods

    /// <summary>
    /// Given a localization string (<paramref name="label"/>) of the form <c>[section.name]</c>, this method extracts
    /// its section and name parts.
    /// </summary>
    /// <param name="label">The localization string to be examined.</param>
    /// <param name="section">The section of the given <paramref name="label"/>.</param>
    /// <param name="name">The name of the given <paramref name="label"/>.</param>
    /// <returns><c>true</c>, if the given <paramref name="label"/> was a localization string and could be processed,
    /// else <c>false</c>.</returns>
    public static bool ExtractSectionAndName(string label, out string section, out string name)
    {
      section = null;
      name = null;
      if (label != null && label.StartsWith("[") &&
          label.EndsWith("]"))
      {
        int pos = label.IndexOf('.');
        if (pos == -1)
          return false;
        section = label.Substring(1, pos - 1).ToLowerInvariant();
        name = label.Substring(pos + 1, label.Length - pos - 2).ToLowerInvariant();
        return true;
      }
      return false;
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

    #endregion

    #region IComparable<StringId> implementation

    public int CompareTo(StringId other)
    {
      return string.Compare(ToString(), other.ToString(), false, ServiceRegistration.Get<ILocalization>().CurrentCulture);
    }

    #endregion

    #region IEquatable<StringId> implementation

    public bool Equals(StringId other)
    {
      return Label == other.Label;
    }

    #endregion

    public override int GetHashCode()
    {
      return _section.GetHashCode() + _name.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (!(obj is StringId))
        return false;
      StringId other = (StringId) obj;
      return other._section == _section && other._name == _name;
    }
  }
}