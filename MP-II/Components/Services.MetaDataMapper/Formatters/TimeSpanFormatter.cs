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
  public class TimeSpanFormatter : IMetaDataFormatter
  {
    #region IMetaDataFormatter Members

    public string Name
    {
      get
      {
        return "timespan";
      }
      set
      {
      }
    }

    TimeSpan GetTimeSpan(object metaData)
    {
      if (metaData == null) return new TimeSpan();
      TimeSpan ts = new TimeSpan();
      if (metaData.GetType() == typeof(TimeSpan))
      {
        ts = (TimeSpan)metaData;
      }
      if (metaData.GetType() == typeof(int))
      {
        int secs = (int)metaData;
        ts = new TimeSpan(0, 0, secs);
      }
      if (metaData.GetType() == typeof(long))
      {
        long secs = (long)metaData;
        ts = new TimeSpan(0, 0, (int)secs);
      }
      if (metaData.GetType() == typeof(uint))
      {
        uint secs = (uint)metaData;
        ts = new TimeSpan(0, 0, (int)secs);
      }
      return ts;
    }
    public string Format(object metaData, string formatting)
    {
      TimeSpan ts = GetTimeSpan(metaData);
      if (String.IsNullOrEmpty(formatting)) return ts.ToString();
      formatting = formatting.Replace("hh", String.Format("{0:00}", ts.Hours));
      formatting = formatting.Replace("mm", String.Format("{0:00}", ts.Minutes));
      formatting = formatting.Replace("ss", String.Format("{0:00}", ts.Seconds));
      return formatting;
    }

    public int CompareTo(object metaData1, object metaData2)
    {
      TimeSpan t1 = GetTimeSpan(metaData1);
      TimeSpan t2 = GetTimeSpan(metaData2);
      return t1.CompareTo(t2);
    }
    #endregion
  }
}
