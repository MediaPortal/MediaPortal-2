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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.Filters
{
  internal class ListFilter : IFilter
  {
    public enum JoinType
    {
      And,
      Or
    }

    public string Field { get; private set; }
    public string Operator { get; private set; }
    public string[] Values { get; private set; }

    public JoinType Type { get; private set; }

    private delegate bool MatchDelegate(object x);

    private PropertyInfo property;
    private MatchDelegate matcher;

    private IEnumerable<int> intValues;
    private IEnumerable<long> longValues;

    public ListFilter(string field, string oper, IEnumerable<string> values, JoinType type)
    {
      Values = values.ToArray();
      Field = field;
      Operator = oper;
      Type = type;
    }

    public void ExpectType(Type type)
    {
      property = type.GetProperty(Field);
      matcher = GetMatchDelegate();
    }

    public bool Matches<T>(T obj)
    {
      return matcher(obj);
    }

    private MatchDelegate GetMatchDelegate()
    {
      if (property.PropertyType == typeof(string))
        return GetStringMatchDelegate();
      if (property.PropertyType == typeof(int))
        return GetIntMatchDelegate();
      if (property.PropertyType == typeof(long))
        return GetLongMatchDelegate();
      if (property.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)))
        return GetListMatchDelegate();

      ServiceRegistration.Get<ILogger>().Error("ListFilter: Cannot load match delegate for field of type '{0}' (property name {1})", property.PropertyType, property.Name);
      throw new ArgumentException("ListFilter: Cannot filter on field of type '{0}'", property.PropertyType.ToString());
    }

    private MatchDelegate GetStringMatchDelegate()
    {
      switch (Operator)
      {
        case "=":
        case "==":
          return x => Values.Contains((string)property.GetValue(x, null));
        case "!=":
          return x => !Values.Contains((string)property.GetValue(x, null));
        case "~=":
          return x =>
          {
            var value = (string)property.GetValue(x, null);
            return Values.Any(w => value.Equals(w, StringComparison.InvariantCultureIgnoreCase));
          };
        case "*=":
          return x =>
          {
            var value = (string)property.GetValue(x, null);
            return Values.Any(w => value.Contains(w, StringComparison.InvariantCultureIgnoreCase));
          };
        case "^=":
          return x =>
          {
            var value = (string)property.GetValue(x, null);
            return Values.Any(w => value.StartsWith(w, StringComparison.InvariantCultureIgnoreCase));
          };
        case "$=":
          return x =>
          {
            var value = (string)property.GetValue(x, null);
            return Values.Any(w => value.EndsWith(w, StringComparison.InvariantCultureIgnoreCase));
          };
        default:
          throw new ParseException("ListFilter: Invalid operator '{0}' for string field", Operator);
      }
    }

    private MatchDelegate GetIntMatchDelegate()
    {
      intValues = Values.Select(stringValue =>
      {
        int outValue = 0;
        if (!Int32.TryParse(stringValue, out outValue))
          throw new ArgumentException("ListFilter: Invalid value '{0}' for integer field", stringValue);
        return outValue;
      });

      switch (Operator)
      {
        case "=":
        case "==":
          return x => intValues.Contains((int)property.GetValue(x, null));
        case "!=":
          return x => !intValues.Contains((int)property.GetValue(x, null));
        default:
          throw new ArgumentException("ListFilter: Invalid list operator '{0}' for integer field", Operator);
      }
    }

    private MatchDelegate GetLongMatchDelegate()
    {
      longValues = Values.Select(stringValue =>
      {
        long outValue = 0;
        if (!Int64.TryParse(stringValue, out outValue))
          throw new ArgumentException("ListFilter: Invalid value '{0}' for integer field", stringValue);
        return outValue;
      });

      switch (Operator)
      {
        case "=":
        case "==":
          return x => longValues.Contains((long)property.GetValue(x, null));
        case "!=":
          return x => !longValues.Contains((long)property.GetValue(x, null));
        default:
          throw new ArgumentException("ListFilter: Invalid list operator '{0}' for integer field", Operator);
      }
    }

    private MatchDelegate GetListMatchDelegate()
    {
      switch (Operator)
      {
        case "*=":
          return delegate(object x)
          {
            var source = ((IEnumerable)property.GetValue(x, null)).Cast<object>().Select(item => item.ToString());
            foreach (var toMatch in Values)
            {
              bool matches = source.Any(item => item == toMatch);
              if (Type == JoinType.And && !matches)
                return false;
              if (Type == JoinType.Or && matches)
                return true;
            }

            return Type == JoinType.And;
          };
        case "==":
          return delegate(object x)
          {
            var source = ((IEnumerable)property.GetValue(x, null)).Cast<object>().Select(item => item.ToString());
            return source.SequenceEqual(Values);
          };
        case "!=":
          return delegate(object x)
          {
            var source = ((IEnumerable)property.GetValue(x, null)).Cast<object>().Select(item => item.ToString());
            return !source.SequenceEqual(Values);
          };
        default:
          throw new ArgumentException("Filter: Invalid operator '{0}' for list field", Operator);
      }
    }
  }
}