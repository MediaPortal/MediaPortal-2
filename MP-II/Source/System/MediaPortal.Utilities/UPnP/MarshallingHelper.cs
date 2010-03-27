#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;

namespace MediaPortal.Utilities.UPnP
{
  public class MarshallingHelper
  {
    public static string SerializeGuidEnumerationToCsv(IEnumerable<Guid> guids)
    {
      return StringUtils.Join(",", guids);
    }

    public static ICollection<Guid> ParseCsvGuidCollection(string csvGuids)
    {
      if (string.IsNullOrEmpty(csvGuids))
        return null;
      string[] guids = csvGuids.Split(',');
      ICollection<Guid> result = new List<Guid>(guids.Length);
      foreach (string guidStr in guids)
        result.Add(new Guid(guidStr));
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