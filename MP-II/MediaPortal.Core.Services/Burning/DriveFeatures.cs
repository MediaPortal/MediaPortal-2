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

namespace MediaPortal.Services.Burning
{
  public class DriveFeatures
  {
    bool fReadsCDR;
    bool fWriteCDR;
    bool fReadsCDRW;
    bool fWriteCDRW;
    bool fReadsDVDRom;
    bool fReadsDVDR;
    bool fWriteDVDR;
    bool fReadsDVDRam;
    bool fWriteDVDRam;
    bool fReadsBRRom;    

    bool fAllowsDummyWrite;
    bool fSupportsBurnFree;

    string fMaxReadSpeed;
    string fMaxWriteSpeed;

    public DriveFeatures(bool aReadsCDR, bool aWriteCDR, bool aReadsCDRW, bool aWriteCDRW, bool aReadsDVDRom, bool aReadsDVDR, bool aWriteDVDR, bool aReadsDVDRam, bool aWriteDVDRam, bool aReadsBRRom, bool aAllowsDummyWrite, bool aSupportsBurnFree, string aMaxReadSpeed, string aMaxWriteSpeed)
    {
      fReadsCDR = aReadsCDR;
      fWriteCDR = aWriteCDR;
      fReadsCDRW = aReadsCDRW;
      fWriteCDRW = aWriteCDRW;
      fReadsDVDRom = aReadsDVDRom;
      fReadsDVDR = aReadsDVDR;
      fWriteDVDR = aWriteDVDR;
      fReadsDVDRam = aReadsDVDRam;
      fWriteDVDRam = aWriteDVDRam;
      fReadsBRRom = aReadsBRRom;
      fAllowsDummyWrite = aAllowsDummyWrite;
      fSupportsBurnFree = aSupportsBurnFree;
      fMaxReadSpeed = aMaxReadSpeed;
      fMaxWriteSpeed = aMaxWriteSpeed;
    }  

    public bool ReadsCDR
    {
      get { return fReadsCDR; }
      set { fReadsCDR = value; }
    }

    public bool WriteCDR
    {
      get { return fWriteCDR; }
      set { fWriteCDR = value; }
    }

    public bool ReadsCDRW
    {
      get { return fReadsCDRW; }
      set { fReadsCDRW = value; }
    }

    public bool WriteCDRW
    {
      get { return fWriteCDRW; }
      set { fWriteCDRW = value; }
    }

    public bool ReadsDVDRom
    {
      get { return fReadsDVDRom; }
      set { fReadsDVDRom = value; }
    }

    public bool ReadsDVDR
    {
      get { return fReadsDVDR; }
      set { fReadsDVDR = value; }
    }

    public bool WriteDVDR
    {
      get { return fWriteDVDR; }
      set { fWriteDVDR = value; }
    }

    public bool ReadsDVDRam
    {
      get { return fReadsDVDRam; }
      set { fReadsDVDRam = value; }
    }

    public bool WriteDVDRam
    {
      get { return fWriteDVDRam; }
      set { fWriteDVDRam = value; }
    }

    public bool ReadsBRRom
    {
      get { return fReadsBRRom; }
      set { fReadsBRRom = value; }
    }

    public bool AllowsDummyWrite
    {
      get { return fAllowsDummyWrite; }
      set { fAllowsDummyWrite = value; }
    }

    public bool SupportsBurnFree
    {
      get { return fSupportsBurnFree; }
      set { fSupportsBurnFree = value; }
    }

    public string MaxReadSpeed
    {
      get { return fMaxReadSpeed; }
      set { fMaxReadSpeed = value; }
    }

    public string MaxWriteSpeed
    {
      get { return fMaxWriteSpeed; }
      set { fMaxWriteSpeed = value; }
    }

    public int MaxReadSpeedInt
    {
      get
      {
        return ParseSpeed(fMaxReadSpeed);
      }
    }

    public int MaxWriteSpeedInt
    {
      get
      {
        return ParseSpeed(fMaxWriteSpeed);
      }
    }


    private int ParseSpeed(string aSpeedStdOutLine)
    {
      // 9173 kB/s (CD  52x, DVD  6x, BD  2x)      
      int resultNumber = 0;
      int parsePos = -1;

      try
      {
        parsePos = aSpeedStdOutLine.IndexOf("kB");
        if (parsePos > 0)
        {
          aSpeedStdOutLine = aSpeedStdOutLine.Remove(parsePos - 1).Trim();
          resultNumber = Convert.ToInt32(aSpeedStdOutLine);
        }

      }
      catch (Exception)
      {
      }
      return resultNumber;
    }

    //public bool IsLocked
    //{
    //  get { return fIsLocked; }
    //}

    /* Sample output of "cdrecord.exe dev=x,x,x -prcap for dvd burner"
      
    Does read CD-R media
    Does write CD-R media
    Does read CD-RW media
    Does write CD-RW media
    Does read DVD-ROM media
    Does read DVD-R media
    Does write DVD-R media
    Does not read DVD-RAM media
    Does not write DVD-RAM media
    Does support test writing

    Does read Mode 2 Form 1 blocks
    Does read Mode 2 Form 2 blocks
    Does read digital audio blocks
    Does restart non-streamed digital audio reads accurately
    Does support Buffer-Underrun-Free recording
    Does read multi-session CDs
    Does read fixed-packet CD media using Method 2
    Does not read CD bar code
    Does read R-W subcode information
    Does not return R-W subcode de-interleaved and error-corrected
    Does read raw P-W subcode data from lead in
    Does return CD media catalog number
    Does return CD ISRC information
    Does support C2 error pointers
    Does not deliver composite A/V data

    Does play audio CDs
    Number of volume control levels: 256
    Does support individual volume control setting for each channel
    Does support independent mute setting for each channel
    Does not support digital output on port 1
    Does not support digital output on port 2

    Loading mechanism type: tray
    Does support ejection of CD via START/STOP command
    Does not lock media on power up via prevent jumper
    Does allow media to be locked in the drive via PREVENT/ALLOW command
    Is not currently in a media-locked state
    Does not support changing side of disk
    Does not have load-empty-slot-in-changer feature
    Does not support Individual Disk Present feature
    
    
    Supported profiles according to MMC-4 feature list:
    Current: DVD+RW
    Profile: DVD+R/DL
    Profile: DVD+R
    Profile: DVD+RW (current)
    Profile: DVD-RW sequential overwrite
    Profile: DVD-RW restricted overwrite
    Profile: DVD-R sequential recording
    Profile: DVD-ROM
    Profile: CD-RW
    Profile: CD-R
    Profile: CD-ROM

    Supported features according to MMC-4 feature list:
    Feature: 'Profile List' (current) (persistent)
    Feature: 'Core' (current) (persistent)
    Feature: 'Morphing' (current) (persistent)
    Feature: 'Removable Medium' (current) (persistent)
    Feature: 'Write Protect'
    Feature: 'Random Readable' (current)
    Feature: 'Multi Read'
    Feature: 'CD Read'
    Feature: 'DVD Read' (current)
    Feature: 'Random Writable' (current)
    Feature: 'Incremental Streaming Writable'
    Feature: 'Formattable' (current)
    Feature: 'Restricted Overwrite'
    Feature: 'DVD+RW' (current)
    Feature: 'DVD+R'
    Feature: 'Rigid Restricted Overwrite'
    Feature: 'CD Track at Once'
    Feature: 'CD Mastering'
    Feature: 'DVD-R/-RW Write'
    Feature: 'DVD+R/DL Read'
    Feature: 'Power Management' (current) (persistent)
    Feature: 'CD Audio analog play'
    Feature: 'Time-out' (current) (persistent)
    Feature: 'DVD-CSS'
    Feature: 'Real Time Streaming' (current)
    Feature: 'Logical Unit Serial Number' (current) (persistent)    Serial: '42BBM27S211 '
    Feature: 'Disk Control Blocks' (current)
    Feature: 'DVD CPRM'
    
    Maximum read  speed:  7056 kB/s (CD  40x, DVD  5x)
    Current read  speed:  7056 kB/s (CD  40x, DVD  5x)
    Maximum write speed:  5645 kB/s (CD  32x, DVD  4x)
    Current write speed:  5645 kB/s (CD  32x, DVD  4x)
    Rotational control selected: CLV/PCAV
    Buffer size in KB: 2048
    Copy management revision supported: 1
    Number of supported write speeds: 6
    Write speed # 0:  5645 kB/s CLV/PCAV (CD  32x, DVD  4x)
    Write speed # 1:  4234 kB/s CLV/PCAV (CD  24x, DVD  3x)
    Write speed # 2:  3528 kB/s CLV/PCAV (CD  20x, DVD  2x)
    Write speed # 3:  2822 kB/s CLV/PCAV (CD  16x, DVD  2x)
    Write speed # 4:  1411 kB/s CLV/PCAV (CD   8x, DVD  1x)
    Write speed # 5:   706 kB/s CLV/PCAV (CD   4x, DVD  0x)
   */


    /* Sample output of "cdrecord.exe dev=x,x,x -media-info -v -v for Blueray reader"   

    Cdrecord-ProDVD-ProBD-Clone 2.01.01a36 (i686-pc-cygwin) Copyright (C) 1995-2007

    Jörg Schilling
    TOC Type: 1 = CD-ROM
    scsidev: '1,0,0'
    scsibus: 1 target: 0 lun: 0
    Using libscg version 'schily-0.9'.
    Using libscg transport code version 'schily-SPTI-scsi-wnt.c-1.46'
    SCSI buffer size: 64512
    atapi: -1
    Device type    : Removable CD-ROM
    Version        : 0
    Response Format: 2
    Capabilities   :
    Vendor_info    : 'MATSHITA'
    Identifikation : 'BD-CMB UJ-120   '
    Revision       : '1.00'
    Device seems to be: Generic mmc2 DVD-R/DVD-RW/DVD-RAM.
    Current: BD-ROM
    Profile: BD-ROM (current)
    Profile: DVD-RAM
    Profile: DVD+R/DL
    Profile: DVD+R
    Profile: DVD+RW
    Profile: DVD-RW restricted overwrite
    Profile: DVD-RW sequential recording
    Profile: DVD-R/DL layer jump recording
    Profile: DVD-R/DL sequential recording
    Profile: DVD-R sequential recording
    Profile: DVD-ROM
    Profile: CD-RW
    Profile: CD-R
    Profile: CD-ROM
    Profile: Removable Disk
    Feature: 'Profile List' (current) (persistent)
    Feature: 'Core' (current) (persistent)
    Feature: 'Morphing' (current) (persistent)
    Feature: 'Removable Medium' (current) (persistent)
    Feature: 'Write Protect'
    Feature: 'Random Readable' (current)
    Feature: 'Multi Read'
    Feature: 'CD Read'
    Feature: 'DVD Read'
    Feature: 'Random Writable'
    Feature: 'Incremental Streaming Writable'
    Feature: 'Formattable'
    Feature: 'Defect Management'
    Feature: 'Restricted Overwrite'
    Feature: 'DVD+RW'
    Feature: 'DVD+R'
    Feature: 'Rigid Restricted Overwrite'
    Feature: 'CD Track at Once'
    Feature: 'CD Mastering'
    Feature: 'DVD-R/-RW Write'
    Feature: 'Layer Jump Recording'
    Feature: 'CD-RW Write'
    Feature: 'DVD+R/DL Read'
    Feature: 'BD Read' (current)
    Feature: 'Power Management' (current) (persistent)
    Feature: 'S.M.A.R.T.'
    Feature: 'CD Audio analog play'
    Feature: 'Microcode Upgrade'
    Feature: 'Time-out' (current) (persistent)
    Feature: 'DVD-CSS'
    Feature: 'Real Time Streaming' (current)
    Feature: 'Logical Unit Serial Number' (current) (persistent)    Serial: 'HD15  0
    04199'
    Feature: 'Disk Control Blocks'
    Feature: 'DVD CPRM'
    Feature: 'AACS' (current)
    cdrecord: Found unsupported 0x40 profile.
    cdrecord: Sorry, no supported CD/DVD/BD-Recorder found on this target.
     */
  }
}
