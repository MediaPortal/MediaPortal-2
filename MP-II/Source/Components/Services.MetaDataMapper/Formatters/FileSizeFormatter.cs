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
  public class FileSizeFormatter : IMetaDataFormatter
  {
    #region IMetaDataFormatter Members

    public string Name
    {
      get
      {
        return "filesize";
      }
      set
      {
      }
    }
    long GetSize(object metaData)
    {
      if (metaData == null) return 0;
      long dwFileSize = 0;

      if (metaData.GetType() == typeof(int))
      {
        dwFileSize = ((long)((int)metaData));
      }
      if (metaData.GetType() == typeof(long))
      {
        dwFileSize = ((long)((long)metaData));
      }
      if (metaData.GetType() == typeof(ulong))
      {
        dwFileSize = ((long)((ulong)metaData));
      }
      if (metaData.GetType() == typeof(uint))
      {
        dwFileSize = ((long)((uint)metaData));
      }
      if (metaData.GetType() == typeof(short))
      {
        dwFileSize = ((long)((short)metaData));
      }
      return dwFileSize;
    }

    public string Format(object metaData, string formatting)
    {
      long dwFileSize = GetSize(metaData);
      if (dwFileSize < 0)
      {
        return "0";
      }
      string szTemp;
      // file < 1 kbyte?
      if (dwFileSize < 1024)
      {
        //  substract the integer part of the float value
        float fRemainder = (((float)dwFileSize) / 1024.0f) - (((float)dwFileSize) / 1024.0f);
        float fToAdd = 0.0f;
        if (fRemainder < 0.01f)
        {
          fToAdd = 0.1f;
        }
        szTemp = String.Format("{0:f} KB", (((float)dwFileSize) / 1024.0f) + fToAdd);
        return szTemp;
      }
      long iOneMeg = 1024 * 1024;

      // file < 1 megabyte?
      if (dwFileSize < iOneMeg)
      {
        szTemp = String.Format("{0:f} KB", ((float)dwFileSize) / 1024.0f);
        return szTemp;
      }

      // file < 1 GByte?
      long iOneGigabyte = iOneMeg;
      iOneGigabyte *= (long)1000;
      if (dwFileSize < iOneGigabyte)
      {
        szTemp = String.Format("{0:f} MB", ((float)dwFileSize) / ((float)iOneMeg));
        return szTemp;
      }
      //file > 1 GByte
      int iGigs = 0;
      while (dwFileSize >= iOneGigabyte)
      {
        dwFileSize -= iOneGigabyte;
        iGigs++;
      }
      float fMegs = ((float)dwFileSize) / ((float)iOneMeg);
      fMegs /= 1000.0f;
      fMegs += iGigs;
      szTemp = String.Format("{0:f} GB", fMegs);
      return szTemp;
    }

    public int CompareTo(object metaData1, object metaData2)
    {
      long dwFileSize1 = GetSize(metaData1);
      long dwFileSize2 = GetSize(metaData2);
      return dwFileSize1.CompareTo(dwFileSize2);
    }
    #endregion
  }
}
