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

namespace MediaPortal.Plugins.MP2Extended.Filters
{
  public static class AutoSuggestion
  {
    public static IEnumerable<string> GetValuesForField<T>(string field, IEnumerable<T> items)
    {
      var property = typeof(T).GetProperty(field);
      if (property.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)) && property.PropertyType != typeof(string))
      {
        return items
          .SelectMany<T, object>(x => property.GetValue(x, null) as IEnumerable<object>)
          .Select(x => x.ToString())
          .Distinct();
      }

      return items
        .Select(x => property.GetValue(x, null).ToString())
        .Distinct();
    }

    public static IEnumerable<string> GetValuesForField<T>(string field, IEnumerable<T> items, string op, int? limit)
    {
      var values = GetValuesForField(field, items);
      if (String.IsNullOrWhiteSpace(op) || !limit.HasValue)
        return values;

      switch (op)
      {
        case "^=":
          return values.Select(s => s.Substring(0, limit.Value > s.Length ? s.Length : limit.Value)).Distinct();
        case "$=":
          return values.Select(input =>
          {
            int takeLength = limit.Value > input.Length ? input.Length : limit.Value;
            return input.Substring(input.Length - takeLength, takeLength);
          }).Distinct();
        default:
          return values;
      }
    }
  }
}