#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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

using MediaPortal.Core.DeviceManager;

namespace MediaPortal.Services.Burning
{
  public class MediaTypeSupport
  {
    bool fWriteDlDVDRam;
    bool fWriteDVDRam;

    bool fWriteDlDVDplusR;
    bool fWriteDVDplusR;
    bool fWriteDVDplusRW;

    bool fWriteDlDVDminusR;
    bool fWriteDVDminusRW;
    bool fWriteDVDminusR;

    bool fWriteCDRW;
    bool fWriteCDR;

    public MediaTypeSupport(bool aWriteDlDVDRam, bool aWriteDVDRam, bool aWriteDlDVDplusR, bool aWriteDVDplusR, bool aWriteDVDplusRW, bool aWriteDlDVDminusR, bool aWriteDVDminusRW, bool aWriteDVDminusR, bool aWriteCDRW, bool aWriteCDR)
    {
      fWriteDlDVDRam = aWriteDlDVDRam;
      fWriteDVDRam = aWriteDVDRam;
      fWriteDlDVDplusR = aWriteDlDVDplusR;
      fWriteDVDplusR = aWriteDVDplusR;
      fWriteDVDplusRW = aWriteDVDplusRW;
      fWriteDlDVDminusR = aWriteDlDVDminusR;
      fWriteDVDminusRW = aWriteDVDminusRW;
      fWriteDVDminusR = aWriteDVDminusR;
      fWriteCDRW = aWriteCDRW;
      fWriteCDR = aWriteCDR;
    }

    public bool WriteDlDVDRam
    {
      get { return fWriteDlDVDRam; }
      set { fWriteDlDVDRam = value; }
    }

    public bool WriteDVDRam
    {
      get { return fWriteDVDRam; }
      set { fWriteDVDRam = value; }
    }

    public bool WriteDlDVDplusR
    {
      get { return fWriteDlDVDplusR; }
      set { fWriteDlDVDplusR = value; }
    }

    public bool WriteDVDplusR
    {
      get { return fWriteDVDplusR; }
      set { fWriteDVDplusR = value; }
    }

    public bool WriteDVDplusRW
    {
      get { return fWriteDVDplusRW; }
      set { fWriteDVDplusRW = value; }
    }

    public bool WriteDlDVDminusR
    {
      get { return fWriteDlDVDminusR; }
      set { fWriteDlDVDminusR = value; }
    }

    public bool WriteDVDminusRW
    {
      get { return fWriteDVDminusRW; }
      set { fWriteDVDminusRW = value; }
    }

    public bool WriteDVDminusR
    {
      get { return fWriteDVDminusR; }
      set { fWriteDVDminusR = value; }
    }

    public bool WriteCDRW
    {
      get { return fWriteCDRW; }
      set { fWriteCDRW = value; }
    }

    public bool WriteCDR
    {
      get { return fWriteCDR; }
      set { fWriteCDR = value; }
    }

    public bool IsMediaTypeSupported(MediaType aMedia)
    {
      switch (aMedia)
      {
        // set to false here so validity checks will fail if no disc is inserted
        case MediaType.None:
          return false;
        case MediaType.ReadOnly:
          return false;
        case MediaType.CDR:
          return fWriteCDR;
        case MediaType.CDRW:
          return fWriteCDRW;
        case MediaType.DVDplusR:
          return fWriteDVDplusR;
        case MediaType.DVDminusR:
          return fWriteDVDminusR;
        case MediaType.DVDplusRW:
          return fWriteDVDplusRW;
        case MediaType.DVDminusRW:
          return fWriteDVDminusRW;
        case MediaType.DlDVDplusR:
          return fWriteDlDVDplusR;
        case MediaType.DlDVDminusR:
          return fWriteDlDVDminusR;
        case MediaType.DlDVDplusRW:
          return false; // ToDo
        case MediaType.DlDVDminusRW:
          return false; // ToDo
        case MediaType.DVDRam:
          return fWriteDVDRam;
        case MediaType.DlDVDRam:
          return fWriteDlDVDRam;
        default:
          return false;
      }
    }
  }
}
