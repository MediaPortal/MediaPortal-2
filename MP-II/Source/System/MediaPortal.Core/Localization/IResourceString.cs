#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace MediaPortal.Core.Localization
{
  /// <summary>
  /// Classes implementing this interface are able to return a string which comes from some resource.
  /// The string might be localized.
  /// Classes implementing this interface might override the <see cref="object.ToString"/>
  /// method in that way that it returns the same string as <see cref="Evaluate"/>, but they are not
  /// forced to. So don't use <see cref="object.ToString"/> to get the resource string, use
  /// <see cref="Evaluate"/> instead!
  /// </summary>
  public interface IResourceString : IComparable<IResourceString>
  {
    /// <summary>
    /// Returns a string representing the string resource, which can be used in the GUI, for example.
    /// The returned string might be localised to the user's culture and regional settings.
    /// </summary>
    /// <param name="args">Additional params to be filled into the resource string. The resource needs
    /// to contain placeholders (<see cref="string.Format(string,object[])"/>)</param>
    /// <returns>String to be used.</returns>
    string Evaluate(params string[] args);
  }
}
