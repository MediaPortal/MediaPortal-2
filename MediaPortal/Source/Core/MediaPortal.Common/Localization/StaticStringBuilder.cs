#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
  /// Class which implements an <see cref="IResourceString"/> for a fixed string.
  /// </summary>
  public class StaticStringBuilder : IResourceString
  {
    #region Protected fields

    protected string _stringValue;

    #endregion

    public StaticStringBuilder(string stringValue)
    {
      _stringValue = stringValue ?? string.Empty;
    }

    #region IResourceString implementation

    public string Evaluate(params string[] args)
    {
      try
      {
        return string.Format(_stringValue, args);
      }
      catch (FormatException)
      {
        return _stringValue;
      }
    }

    public int CompareTo(IResourceString other)
    {
      return _stringValue.CompareTo(other.Evaluate());
    }

    #endregion

    public override string ToString()
    {
      return Evaluate();
    }

    public override int GetHashCode()
    {
      return _stringValue.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (!(obj is StaticStringBuilder))
        return false;
      StaticStringBuilder other = (StaticStringBuilder) obj;
      return other._stringValue == _stringValue;
    }
  }
}