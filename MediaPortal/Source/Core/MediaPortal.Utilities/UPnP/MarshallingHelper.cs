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
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Utilities.UPnP
{
  public class MarshallingHelper
  {
    public static string SerializeGuidEnumerationToCsv(IEnumerable<Guid> guids)
    {
      return StringUtils.Join(",", guids);
    }

    public static string SerializeStringEnumerationToCsv(IEnumerable<string> values)
    {
      return StringUtils.Join(",", values);
    }

    public static string SerializeTuple2EnumerationToCsv(IEnumerable<Tuple<string, string>> values)
    {
      return StringUtils.Join(",", values.Select(t => string.Format("{0};{1}", t.Item1, t.Item2)));
    }

    public static string SerializeTuple3EnumerationToCsv(IEnumerable<Tuple<string, string, string>> values)
    {
      return StringUtils.Join(",", values.Select(t => string.Format("{0};{1};{2}", t.Item1, t.Item2, t.Item3)));
    }

    public static IList<Guid> ParseCsvGuidCollection(string csvGuids)
    {
      if (string.IsNullOrEmpty(csvGuids))
        return null;
      string[] guids = csvGuids.Split(',');
      IList<Guid> result = new List<Guid>(guids.Length);
      foreach (string guidStr in guids)
        result.Add(new Guid(guidStr));
      return result;
    }

    public static IList<string> ParseCsvStringCollection(string csvValues)
    {
      return string.IsNullOrEmpty(csvValues) ? null : new List<string>(csvValues.Split(','));
    }

    public static IList<Tuple<string, string>> ParseCsvTuple2Collection(string csvTupleValues)
    {
      if(string.IsNullOrEmpty(csvTupleValues))
        return null;
      string[] tuples = csvTupleValues.Split(',');
      IList<Tuple<string, string>> result = new List<Tuple<string, string>>(tuples.Length);
      foreach (string tupleStr in tuples)
      {
        string[] tupleSplit = tupleStr.Split(';');
        result.Add(new Tuple<string, string>(tupleSplit[0], tupleSplit[1]));
      }
      return result;
    }

    public static IList<Tuple<string, string, string>> ParseCsvTuple3Collection(string csvTupleValues)
    {
      if (string.IsNullOrEmpty(csvTupleValues))
        return null;
      string[] tuples = csvTupleValues.Split(',');
      IList<Tuple<string, string, string>> result = new List<Tuple<string, string, string>>(tuples.Length);
      foreach (string tupleStr in tuples)
      {
        string[] tupleSplit = tupleStr.Split(';');
        result.Add(new Tuple<string, string, string>(tupleSplit[0], tupleSplit[1], tupleSplit[2]));
      }
      return result;
    }

    public static string SerializeGuid(Guid guid)
    {
      return guid.ToString("B");
    }

    public static Guid DeserializeGuid(string guidStr)
    {
      return new Guid(guidStr);
    }
  }
}
