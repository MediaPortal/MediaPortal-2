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

using MediaPortal.Core.DeviceManager;

namespace MediaPortal.Services.Burning
{
  #region enums
  public enum BlankStatus
  {
    empty = 0,
    complete = 1,
    // session?
  }

  public enum FormatStatus
  {
    none = 0,
  }
  #endregion

  /// <summary>
  /// The MediaInfo contains information about the type (like CD-R, DVD+RW, etc.), Sessions and Blank-Status
  /// </summary>
  public class MediaInfo
  {
    #region fields
    MediaType fCurrentMediaType = MediaType.None;
    bool fIsErasable = false;
    string fDataType = "standard";
    BlankStatus fDiskStatus = BlankStatus.empty;
    BlankStatus fSessionStatus = BlankStatus.empty;
    FormatStatus fBgFormatStatus = FormatStatus.none;
    int fFirstTrack = 1;
    int fTotalSessions = 1;
    bool fIsRestricted = false;
    long fSize = 0;
    #endregion

    #region constructor
    public MediaInfo(MediaType aMediaType, bool aIsErasable, string aDataType, BlankStatus aDiskStatus, BlankStatus aSessionStatus, FormatStatus aBgFormatStatus, int aFirstTrack, int aTotalSessions, bool aIsRestricted, long aSize)
    {
      fCurrentMediaType = aMediaType;
      fIsErasable = aIsErasable;
      fDataType = aDataType;
      fDiskStatus = aDiskStatus;
      fSessionStatus = aSessionStatus;
      fBgFormatStatus = aBgFormatStatus;
      fFirstTrack = aFirstTrack;
      fTotalSessions = aTotalSessions;
      fIsRestricted = aIsRestricted;
      fSize = aSize;
    }
    #endregion

    #region getters and setters
    /// <summary>
    /// This will output the media type like you see it on the box
    /// </summary>
    public string HumanMediaString
    {
      get 
      {
        switch (fCurrentMediaType)
        {
          case MediaType.None:
            return "None";
          case MediaType.ReadOnly:
            return "Read only";
          case MediaType.CDR:
            return "CD-R";
          case MediaType.CDRW:
            return "CR-RW";
          case MediaType.DVDplusR:
            return "DVD+R";
          case MediaType.DVDminusR:
            return "DVD-R";
          case MediaType.DVDplusRW:
            return "DVD+RW";
          case MediaType.DVDminusRW:
            return "DVD-RW";
          case MediaType.DlDVDplusR:
            return "Double Layer DVD+R";
          case MediaType.DlDVDminusR:
            return "Double Layer DVD-R";
          case MediaType.DlDVDplusRW:
            return "Double Layer DVD+RW";
          case MediaType.DlDVDminusRW:
            return "Double Layer DVD-RW";
          case MediaType.DVDRam:
            return "DVD-RAM";
          case MediaType.DlDVDRam:
            return "Double Layer DVD-RAM";
          default:
            return "Unknown!";            
        }        
      }      
    }

    public MediaType CurrentMediaType
    {
      get { return fCurrentMediaType; }
      set { fCurrentMediaType = value; }
    }

    public CapacityType CurrentCapacityType
    {
      get
      {
        switch (fCurrentMediaType)
        {
          case MediaType.None:
            return CapacityType.Unknown;
          case MediaType.ReadOnly:
            return CapacityType.Unknown;
          case MediaType.CDR:
            return CapacityType.CDR;
          case MediaType.CDRW:
            return CapacityType.CDR;
          case MediaType.DVDplusR:
            return CapacityType.DVDR;
          case MediaType.DVDminusR:
            return CapacityType.DVDR;
          case MediaType.DVDplusRW:
            return CapacityType.DVDR;
          case MediaType.DVDminusRW:
            return CapacityType.DVDR;
          case MediaType.DlDVDplusR:
            return CapacityType.DualDVDR;
          case MediaType.DlDVDminusR:
            return CapacityType.DualDVDR;
          case MediaType.DlDVDplusRW:
            return CapacityType.DualDVDR;
          case MediaType.DlDVDminusRW:
            return CapacityType.DualDVDR;
          case MediaType.DVDRam:
            return CapacityType.DVDR;
          case MediaType.DlDVDRam:
            return CapacityType.DualDVDR;
          default:
            return CapacityType.Unknown;
        }
      }
    }

    public bool IsErasable
    {
      get { return fIsErasable; }
      set { fIsErasable = value; }
    }

    public string DataType
    {
      get { return fDataType; }
      set { fDataType = value; }
    }

    public BlankStatus DiskStatus
    {
      get { return fDiskStatus; }
      set { fDiskStatus = value; }
    }

    public BlankStatus SessionStatus
    {
      get { return fSessionStatus; }
      set { fSessionStatus = value; }
    }

    public FormatStatus BgFormatStatus
    {
      get { return fBgFormatStatus; }
      set { fBgFormatStatus = value; }
    }

    public int FirstTrack
    {
      get { return fFirstTrack; }
      set { fFirstTrack = value; }
    }

    public int TotalSessions
    {
      get { return fTotalSessions; }
      set { fTotalSessions = value; }
    }

    public bool IsRestricted
    {
      get { return fIsRestricted; }
      set { fIsRestricted = value; }
    }

    public long Size
    {
      get { return fSize; }
      set { fSize = value; }
    }
    #endregion

    /*
    Using generic SCSI-3/mmc-3 DVD+RW driver (mmc_dvdplusrw).
    Driver flags   : DVD MMC-3 SWABAUDIO BURNFREE
    Supported modes: PACKET SAO
    Disk Is erasable
    data type:                standard
    disk status:              empty
    session status:           empty
    BG format status:         none
    first track:              1
    number of sessions:       1
    first track in last sess: 1
    last track in last sess:  1
    Disk Is not unrestricted
    Disk type: DVD, HD-DVD or BD

    Track  Sess Type   Start Addr End Addr   Size
    ==============================================
        1     1 Blank  0          2295103    2295104

    Next writable address:              0
    Remaining writable size:            2295104
    */
  }
}
