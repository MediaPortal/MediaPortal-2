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

using System.Collections.Generic;

namespace MediaPortal.SkinEngine.MpfElements.Resources
{
  /// <summary>
  /// Class to wrap a value object which cannot directly be used. Objects of this class will NOT be
  /// automatically converted to the underlaying <see cref="Value"/> object. That's why the code where
  /// instances of this class are used must explicitly support <see cref="LateBoundValue"/>s.
  /// </summary>
  public class LateBoundValue : ValueWrapper
  {
    #region Ctor

    public LateBoundValue() { }

    // We don't expose the LateBoundValue(object value) constructor to avoid the misusage
    // for {LateBoundValue {Binding ...}} (which cannot work, because the binding cannot bind
    // when used as a constructor parameter)

    #endregion

    public static IList<object> ConvertLateBoundValues(IEnumerable<object> parameters)
    {
      IList<object> result = new List<object>();
      foreach (object parameter in parameters)
        if (parameter is LateBoundValue)
          result.Add(((LateBoundValue) parameter).Value);
        else
          result.Add(parameter);
      return result;
    }
  }
}
