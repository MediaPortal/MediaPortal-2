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

using MediaPortal.DeviceManager;

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

    #region static methods

    public static bool CheckInsertedMediaType(ProjectType aProjectType, Burner aSelectedBurner)
    {
      CapacityType CurrentType = aSelectedBurner.CurrentMediaInfo.CurrentCapacityType;

      switch (aProjectType)
      {
        case ProjectType.Autoselect:
          return CurrentType != CapacityType.Unknown;
        case ProjectType.DataCD:
          return CurrentType == CapacityType.CDR;
        case ProjectType.AudioCD:
          return CurrentType == CapacityType.CDR;
        case ProjectType.PhotoCD:
          return CurrentType == CapacityType.CDR;
        case ProjectType.IsoCD:
          return CurrentType == CapacityType.CDR;
        case ProjectType.DataDVD:
          return CurrentType == CapacityType.DVDR;
        case ProjectType.VideoDVD:
          return CurrentType == CapacityType.DVDR;
        case ProjectType.IsoDVD:
          return CurrentType == CapacityType.DVDR;
        case ProjectType.LargeDataDVD:
          return CurrentType == CapacityType.DualDVDR;
        case ProjectType.LargeIsoDVD:
          return CurrentType == CapacityType.DualDVDR;
        default:
          return false;
      }
    }

    /// <summary>
    /// Checks whether the needed Drive is present
    /// </summary>
    /// <param name="aProjectType">The ProjectType like Audio-CD, Video-DVD, etc.</param>
    /// <param name="aSelectedBurner">The drive to check</param>
    /// <returns>Whether the given drive could handle the project's files</returns>
    public static bool CheckBurnerRequirements(ProjectType aProjectType, Burner aSelectedBurner)
    {
      switch (aProjectType)
      {
        case ProjectType.Autoselect:
          return aSelectedBurner.MediaFeatures.WriteCDR;
        case ProjectType.DataCD:
          return aSelectedBurner.MediaFeatures.WriteCDR;
        case ProjectType.AudioCD:
          return aSelectedBurner.MediaFeatures.WriteCDR;
        case ProjectType.PhotoCD:
          return aSelectedBurner.MediaFeatures.WriteCDR;
        case ProjectType.IsoCD:
          return aSelectedBurner.MediaFeatures.WriteCDR;
        case ProjectType.DataDVD:
          return (aSelectedBurner.MediaFeatures.WriteDVDplusR || aSelectedBurner.MediaFeatures.WriteDVDminusR);
        case ProjectType.VideoDVD:
          return (aSelectedBurner.MediaFeatures.WriteDVDplusR || aSelectedBurner.MediaFeatures.WriteDVDminusR);
        case ProjectType.IsoDVD:
          return (aSelectedBurner.MediaFeatures.WriteDVDplusR || aSelectedBurner.MediaFeatures.WriteDVDminusR);
        case ProjectType.LargeDataDVD:
          return (aSelectedBurner.MediaFeatures.WriteDlDVDplusR || aSelectedBurner.MediaFeatures.WriteDlDVDminusR);
        case ProjectType.LargeIsoDVD:
          return (aSelectedBurner.MediaFeatures.WriteDlDVDplusR || aSelectedBurner.MediaFeatures.WriteDlDVDminusR);
        default:
          return false;
      }
    }

    public static int GetMediaSizeMbByType(MediaType aMediaType)
    {
      switch (aMediaType)
      {
        case MediaType.None:
          return 0;
        case MediaType.ReadOnly:
          return 0;
        case MediaType.CDR:
          return 700;
        case MediaType.CDRW:
          return 650;
        case MediaType.DVDplusR:
          return 4482;
        case MediaType.DVDminusR:
          return 4482;
        case MediaType.DVDplusRW:
          return 4482;
        case MediaType.DVDminusRW:
          return 4482;
        case MediaType.DlDVDplusR:
          return 8964;
        case MediaType.DlDVDminusR:
          return 8964;
        case MediaType.DlDVDplusRW:
          return 8964;
        case MediaType.DlDVDminusRW:
          return 8964;
        case MediaType.DVDRam: // Type 2
          return 4482;
        case MediaType.DlDVDRam:
          return 8964;
        default:
          return 0;
      }
    }

    public static int GetMaxMediaSizeMbByProjectType(ProjectType aProjectType, Burner aCurrentDrive)
    {
      switch (aProjectType)
      {
        case ProjectType.DataCD:
          return 700;
        case ProjectType.AudioCD:
          return 700;
        case ProjectType.PhotoCD:
          return 700;
        case ProjectType.IsoCD:
          return 700;
        case ProjectType.DataDVD:
          return 4482;
        case ProjectType.VideoDVD:
          return 4482;
        case ProjectType.IsoDVD:
          return 4482;
        case ProjectType.LargeDataDVD:
          return 8964;
        case ProjectType.LargeIsoDVD:
          return 8964;
        case ProjectType.Autoselect:
          if (aCurrentDrive == null)
            return 0;
          else
          {
            if (aCurrentDrive.MediaFeatures.WriteDlDVDplusR || aCurrentDrive.MediaFeatures.WriteDlDVDminusR || aCurrentDrive.MediaFeatures.WriteDlDVDRam)
              return 8964;
            if (aCurrentDrive.MediaFeatures.WriteDVDplusR || aCurrentDrive.MediaFeatures.WriteDVDminusR)
              return 4482;
            else
              return 700;
          }
        default:
          return 0;
      }
    }

    #endregion


  }
}
