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
using MediaPortal.Core.Localisation;

namespace MediaPortal.Core.Properties
{
  /// <summary>
  /// Class which implements an ILabelProperty for a (localized) string
  /// </summary>
  public class SimpleLabelProperty : ILabelProperty
  {
    #region variables

    private StringId _localizedString;
    private string _stringValue;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLabelProperty"/> class.
    /// </summary>
    /// <param name="stringValue">The string value.</param>
    public SimpleLabelProperty(string stringValue)
    {
      _stringValue = stringValue;
      if (StringId.IsString(_stringValue))
      {
        _localizedString = new StringId(_stringValue);
        _stringValue = null;
      }
    }

    #region ILabelProperty Members

    /// <summary>
    /// Evaluates the specified control.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="container">The container.</param>
    /// <returns></returns>
    public string Evaluate(IControl control, IControl container)
    {
      if (_localizedString != null)
      {
        return _localizedString.ToString();
      }
      return _stringValue;
    }

    /// <summary>
    /// Evaluates the specified control.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="container">The container.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public string Evaluate(IControl control, IControl container, string name)
    {
      if (_localizedString != null)
      {
        return _localizedString.ToString();
      }
      return _stringValue;
    }

    #endregion
  }
}
