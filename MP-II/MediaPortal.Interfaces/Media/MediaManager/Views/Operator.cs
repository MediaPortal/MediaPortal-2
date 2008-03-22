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

namespace MediaPortal.Media.MediaManager.Views
{
  public enum SortOrder
  {
    None,
    Ascending,
    Descending
  }
  public class Operator : IEquatable<Operator>
  {
    public static Operator None = new Operator(0);
    public static Operator And = new Operator(1);
    public static Operator Or = new Operator(2);
    public static Operator Same = new Operator(3);
    public static Operator NotEquals = new Operator(4);
    public static Operator GreaterThen = new Operator(5);
    public static Operator LessThen = new Operator(6);
    public static Operator GreaterOrSameThen = new Operator(7);
    public static Operator LessOrSameThen = new Operator(8);
    public static Operator Distinct = new Operator(9);
    public static Operator Like = new Operator(10);
    public static Operator Top = new Operator(11);

    private readonly int _value;

    public Operator(int value)
    {
      _value = value;
    }

    public override string ToString()
    {
      if (this == None)
      {
        return "";
      }
      if (this == And)
      {
        return "and";
      }
      if (this == Or)
      {
        return "or";
      }
      if (this == Same)
      {
        return "=";
      }
      if (this == NotEquals)
      {
        return "!=";
      }
      if (this == GreaterThen)
      {
        return ">";
      }
      if (this == LessThen)
      {
        return "<";
      }
      if (this == GreaterOrSameThen)
      {
        return ">=";
      }
      if (this == LessOrSameThen)
      {
        return "<=";
      }
      if (this == Distinct)
      {
        return "distinct";
      }
      if (this == Like)
      {
        return "like";
      }
      return "";
    }

    public static bool operator ==(Operator left, Operator right)
    {
      return left._value == right._value;
    }

    public static bool operator !=(Operator left, Operator right)
    {
      return left._value != right._value;
    }

    public bool Equals(Operator @operator)
    {
      if (@operator == null)
      {
        return false;
      }
      return _value == @operator._value;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      return Equals(obj as Operator);
    }

    public override int GetHashCode()
    {
      return _value;
    }
  } ;
}
