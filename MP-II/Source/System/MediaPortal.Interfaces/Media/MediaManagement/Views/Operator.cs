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

namespace MediaPortal.Media.MediaManagement.Views
{
  /// <summary>
  /// TODO: not sure yet if this will be kept
  /// </summary>
  public class Operator : IEquatable<Operator>
  {
    public static Operator And = new Operator("AND", 2);
    public static Operator Or = new Operator("OR", 2);
    public static Operator EQ = new Operator("=", 2);
    public static Operator NEQ = new Operator("<>", 2);
    public static Operator GT = new Operator(">", 2);
    public static Operator LT = new Operator("<", 2);
    public static Operator GE = new Operator(">=", 2);
    public static Operator LE = new Operator("<=", 2);
    public static Operator Distinct = new Operator("DISTINCT", 1);
    public static Operator Like = new Operator("LIKE", 1);

    private readonly string _operator;
    private readonly int _cardinality;

    public Operator(string @operator, int cardinality)
    {
      _operator = @operator;
      _cardinality = cardinality;
    }

    public string OperatorStr
    {
      get { return _operator; }
    }

    public int Cardinality
    {
      get { return _cardinality; }
    }

    public static bool operator ==(Operator left, Operator right)
    {
      return left._operator == right._operator;
    }

    public static bool operator !=(Operator left, Operator right)
    {
      return left._operator != right._operator;
    }

    public bool Equals(Operator other)
    {
      if (other == null)
      {
        return false;
      }
      return _operator == other._operator;
    }

    public override string ToString()
    {
      return OperatorStr;
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
      return _operator.GetHashCode();
    }
  } ;
}
