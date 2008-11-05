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
using System.IO;

namespace Models.Media
{
  public class FileUtil
  {
    #region helper functions

    public static string GetFileSize(string fileName, out long size, out DateTime creationTime)
    {
      FileInfo info = new FileInfo(fileName);
      size = info.Length;
      creationTime = info.CreationTime;
      return GetSize(info.Length);
    }

    public static string GetSize(long dwFileSize)
    {
      if (dwFileSize < 0)
      {
        return "0";
      }
      string szTemp;
      // file < 1 kbyte?
      if (dwFileSize < 1024)
      {
        //  substract the integer part of the float value
        float fRemainder = (dwFileSize/1024.0f) - (dwFileSize/1024.0f);
        float fToAdd = 0.0f;
        if (fRemainder < 0.01f)
        {
          fToAdd = 0.1f;
        }
        szTemp = String.Format("{0:f} KB", (dwFileSize/1024.0f) + fToAdd);
        return szTemp;
      }
      long iOneMeg = 1024*1024;

      // file < 1 megabyte?
      if (dwFileSize < iOneMeg)
      {
        szTemp = String.Format("{0:f} KB", dwFileSize/1024.0f);
        return szTemp;
      }

      // file < 1 GByte?
      long iOneGigabyte = iOneMeg;
      iOneGigabyte *= 1000;
      if (dwFileSize < iOneGigabyte)
      {
        szTemp = String.Format("{0:f} MB", dwFileSize/((float) iOneMeg));
        return szTemp;
      }
      //file > 1 GByte
      int iGigs = 0;
      while (dwFileSize >= iOneGigabyte)
      {
        dwFileSize -= iOneGigabyte;
        iGigs++;
      }
      float fMegs = dwFileSize/((float) iOneMeg);
      fMegs /= 1000.0f;
      fMegs += iGigs;
      szTemp = String.Format("{0:f} GB", fMegs);
      return szTemp;
    }

    #endregion
  }
}
