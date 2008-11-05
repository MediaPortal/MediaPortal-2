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
using System.Collections.Generic;
using System.Text;

using MediaPortal.Media.MetaData;

namespace Components.Services.MetaDataMapper.Formatters
{
  public class DateTimeFormatter : IMetaDataFormatter
  {
    #region IMetaDataFormatter Members

    public string Name
    {
      get
      {
        return "date";
      }
      set
      {
      }
    }

    public DateTime GetDate(object metaData)
    {
      if (metaData == null) return DateTime.MinValue;
      if (metaData.GetType() != typeof(DateTime)) return DateTime.MinValue;
      return (DateTime)metaData;
    }
    public string Format(object metaData, string formatting)
    {
      DateTime dt = GetDate(metaData);
      if (String.IsNullOrEmpty(formatting)) return dt.ToString();
      return dt.ToString(formatting);
    }
    public int CompareTo(object metaData1, object metaData2)
    {
      DateTime dt1 = GetDate(metaData1);
      DateTime dt2 = GetDate(metaData2);
      return dt1.CompareTo(dt2);
    }
    #endregion
  }
}
