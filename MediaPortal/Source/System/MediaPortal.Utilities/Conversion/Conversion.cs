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

namespace MediaPortal.Utilities.Conversion
{
  /// <summary>
  /// Contains methods to do all kinds of Conversions
  /// </summary>
  public class Conversion
  {
    #region Date / Time
    public static string SecondsToShortHMSString(int lSeconds)
    {
      if (lSeconds < 0) return ("0:00");
      int hh = lSeconds / 3600;
      lSeconds = lSeconds % 3600;
      int mm = lSeconds / 60;
      //int ss = lSeconds % 60;

      string strHMS = String.Format("{0}:{1:00}", hh, mm);
      return strHMS;
    }

    public static string SecondsToHMSString(TimeSpan timespan)
    {
      return SecondsToHMSString(timespan.Seconds);
    }

    public static string SecondsToHMSString(int lSeconds)
    {
      if (lSeconds < 0) return ("0:00");
      int hh = lSeconds / 3600;
      lSeconds = lSeconds % 3600;
      int mm = lSeconds / 60;
      int ss = lSeconds % 60;

      string strHMS = hh >= 1 ? String.Format("{0}:{1:00}:{2:00}", hh, mm, ss) : String.Format("{0}:{1:00}", mm, ss);
      return strHMS;
    }

    public static string SecondsToHMString(int lSeconds)
    {
      if (lSeconds < 0) return "0:00";
      int hh = lSeconds / 3600;
      lSeconds = lSeconds % 3600;
      int mm = lSeconds / 60;

      string strHM = hh >= 1 ? String.Format("{0:00}:{1:00}", hh, mm) : String.Format("0:{0:00}", mm);
      return strHM;
    }

    public static long GetUnixTime(DateTime desiredTime_)
    {
      TimeSpan ts = (desiredTime_ - new DateTime(1970, 1, 1, 0, 0, 0));

      return (long)ts.TotalSeconds;
    }

    public static DateTime LongToDate(long ldate)
    {
      try
      {
        if (ldate < 0) return DateTime.MinValue;
        int sec = (int)(ldate % 100L); ldate /= 100L;
        int minute = (int)(ldate % 100L); ldate /= 100L;
        int hour = (int)(ldate % 100L); ldate /= 100L;
        int day = (int)(ldate % 100L); ldate /= 100L;
        int month = (int)(ldate % 100L); ldate /= 100L;
        int year = (int)ldate;
        DateTime dt = new DateTime(year, month, day, hour, minute, sec, 0);
        return dt;
      }
      catch (Exception)
      {
        return DateTime.MinValue;
      }
    }

    public static long DateToLong(DateTime dt)
    {
      try
      {
        long iSec = dt.Second;
        long iMin = dt.Minute;
        long iHour = dt.Hour;
        long iDay = dt.Day;
        long iMonth = dt.Month;
        long iYear = dt.Year;

        long lRet = iYear;
        lRet = lRet * 100L + iMonth;
        lRet = lRet * 100L + iDay;
        lRet = lRet * 100L + iHour;
        lRet = lRet * 100L + iMin;
        lRet = lRet * 100L + iSec;
        return lRet;
      }
      catch (Exception)
      {
        return 0;
      }
    }
    #endregion

    #region Size Conversions
    /// <summary>
    /// Returns the size as a string formatted with KB, MB, GB
    /// </summary>
    /// <param name="dwFileSize"></param>
    /// <returns></returns>
    public static string GetSizeString(long dwFileSize)
    {
      if (dwFileSize < 0) return "0";
      string szTemp;
      // file < 1 kbyte?
      if (dwFileSize < 1024)
      {
        //  substract the integer part of the float value
        float fRemainder = (dwFileSize / 1024.0f) - (dwFileSize / 1024.0f);
        float fToAdd = 0.0f;
        if (fRemainder < 0.01f)
          fToAdd = 0.1f;
        szTemp = String.Format("{0:f} KB", (dwFileSize / 1024.0f) + fToAdd);
        return szTemp;
      }
      const long iOneMeg = 1024 * 1024;

      // file < 1 megabyte?
      if (dwFileSize < iOneMeg)
      {
        szTemp = String.Format("{0:f} KB", dwFileSize / 1024.0f);
        return szTemp;
      }

      // file < 1 GByte?
      const long iOneGigabyte = iOneMeg * 1000;
      if (dwFileSize < iOneGigabyte)
      {
        szTemp = String.Format("{0:f} MB", dwFileSize / (float) iOneMeg);
        return szTemp;
      }
      //file > 1 GByte
      int iGigs = 0;
      while (dwFileSize >= iOneGigabyte)
      {
        dwFileSize -= iOneGigabyte;
        iGigs++;
      }
      float fMegs = dwFileSize / (float) iOneMeg;
      fMegs /= 1000.0f;
      fMegs += iGigs;
      szTemp = String.Format("{0:f} GB", fMegs);
      return szTemp;
    }
    #endregion
  }
}
