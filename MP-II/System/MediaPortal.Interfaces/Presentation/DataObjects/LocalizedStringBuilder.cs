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

using MediaPortal.Presentation.Localisation;

namespace MediaPortal.Presentation.DataObjects
{
  /// <summary>
  /// Wrapper class for a <see cref="StringId"/> instance, which implements
  /// <see cref="IResourceString"/> for this localized string.
  /// </summary>
  public class LocalizedStringBuilder : IResourceString
  {
    #region Protected fields

    protected StringId _localizedString;

    #endregion

    /// <summary>
    /// Creates a new instance of <see cref="LocalizedStringBuilder"/>, which is based on
    /// the specified <see cref="StringId"/> value.
    /// </summary>
    public LocalizedStringBuilder(StringId value)
    {
      _localizedString = value;
    }

    public StringId LocalizedString
    {
      get { return _localizedString; }
      set { _localizedString = value; }
    }

    #region IResourceString implementation

    public string Evaluate()
    {
      return _localizedString.ToString();
    }

    public int CompareTo(IResourceString other)
    {
      return Evaluate().CompareTo(other.Evaluate());
    }

    #endregion

    public override string ToString()
    {
      return Evaluate();
    }
  }
}
