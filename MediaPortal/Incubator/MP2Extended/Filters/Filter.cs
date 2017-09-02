#region Copyright (C) 2012-2013 MPExtended

// Copyright (C) 2012-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.Filters
{
  internal class Filter : IFilter
  {
    public string Field { get; private set; }
    public string Operator { get; private set; }
    public string Value { get; private set; }

    private delegate bool MatchDelegate(object value);

    private PropertyInfo property;
    private MatchDelegate matcher;

    private int intValue;
    private long longValue;
    private bool boolValue;

    public Filter(string field, string oper, string value)
    {
      Field = field;
      Operator = oper;
      Value = value;
    }

    public void ExpectType(Type type)
    {
      property = type.GetProperty(Field);
      matcher = GetMatchDelegate();
    }

    public bool Matches<T>(T obj)
    {
      return matcher(property.GetValue(obj, null));
    }

    private MatchDelegate GetMatchDelegate()
    {
      if (property.PropertyType == typeof(string))
        return GetStringMatchDelegate();
      if (property.PropertyType == typeof(int))
        return GetIntMatchDelegate();
      if (property.PropertyType == typeof(long))
        return GetLongMatchDelegate();
      if (property.PropertyType == typeof(bool))
        return GetBoolMatchDelegate();
      if (property.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)))
        return GetListMatchDelegate();

      ServiceRegistration.Get<ILogger>().Error("Filter: Cannot load match delegate for field of type '{0}' (property name {1})", property.PropertyType, property.Name);
      throw new ArgumentException("Filter: Cannot filter on field of type '{0}'", property.PropertyType.ToString());
    }

    private MatchDelegate GetStringMatchDelegate()
    {
      switch (Operator)
      {
        case "=":
        case "==":
          return x => (string)x == Value;
        case "~=":
          return x => ((string)x).Equals(Value, StringComparison.InvariantCultureIgnoreCase);
        case "!=":
          return x => (string)x != Value;
        case "*=":
          return x => ((string)x).Contains(Value, StringComparison.InvariantCultureIgnoreCase);
        case "^=":
          return x => ((string)x).StartsWith(Value, StringComparison.InvariantCultureIgnoreCase);
        case "$=":
          return x => ((string)x).EndsWith(Value, StringComparison.InvariantCultureIgnoreCase);

        default:
          throw new ParseException("Filter: Invalid operator '{0}' for string field", Operator);
      }
    }

    private MatchDelegate GetIntMatchDelegate()
    {
      if (!Int32.TryParse(Value, out intValue))
        throw new ArgumentException("Filter: Invalid value '{0}' for integer field", Value);

      switch (Operator)
      {
        case "=":
        case "==":
          return x => (int)x == intValue;
        case "!=":
          return x => (int)x != intValue;
        case ">":
          return x => (int)x > intValue;
        case ">=":
          return x => (int)x >= intValue;
        case "<":
          return x => (int)x < intValue;
        case "<=":
          return x => (int)x <= intValue;
        default:
          throw new ArgumentException("Filter: Invalid operator '{0}' for integer field", Operator);
      }
    }

    private MatchDelegate GetLongMatchDelegate()
    {
      if (!Int64.TryParse(Value, out longValue))
        throw new ArgumentException("Filter: Invalud value '{0}' for integer field", Value);

      switch (Operator)
      {
        case "=":
        case "==":
          return x => (long)x == longValue;
        case "!=":
          return x => (long)x != longValue;
        case ">":
          return x => (long)x > longValue;
        case ">=":
          return x => (long)x >= longValue;
        case "<":
          return x => (long)x < longValue;
        case "<=":
          return x => (long)x <= longValue;
        default:
          throw new ArgumentException("Filter: Invalid operator '{0}' for integer field", Operator);
      }
    }

    private MatchDelegate GetBoolMatchDelegate()
    {
      boolValue = Value == "true" || Value == "1";
      if (!boolValue && Value != "false" && Value != "0")
        throw new ArgumentException("Filter: Invalid value '{0}' for boolean field", Value);

      switch (Operator)
      {
        case "=":
        case "==":
          return x => (bool)x == boolValue;
        case "!=":
          return x => (bool)x != boolValue;
        default:
          throw new ArgumentException("Filter: Invalid operator '{0}' for boolean field", Operator);
      }
    }

    private MatchDelegate GetListMatchDelegate()
    {
      switch (Operator)
      {
        case "*=":
          return delegate(object x)
          {
            foreach (var item in (IEnumerable)x)
            {
              if (item.ToString() == Value)
                return true;
            }

            return false;
          };
        default:
          throw new ArgumentException("Filter: Invalid operator '{0}' for list field", Operator);
      }
    }
  }
}