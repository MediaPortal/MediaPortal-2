#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
  public class LocalizedLabelProperty : ILabelProperty
  {
    #region variables

    private StringId _localizedString;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleLabelProperty"/> class.
    /// </summary>
    /// <param name="stringValue">The string value.</param>
    public LocalizedLabelProperty(StringId stringValue)
    {
      _localizedString = stringValue;
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
      return _localizedString.ToString();
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
      return _localizedString.ToString();
    }

    #endregion
  }
}