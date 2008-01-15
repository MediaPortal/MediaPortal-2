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
using System.IO;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.DeviceManager;
using MediaPortal.Core.Logging;
using System.Threading;


namespace MediaPortal.Services.Burning
{
  /// <summary>
  /// The BurnManager knows about your drives' status and handles CD/DVD/Blueray disc creation
  /// </summary>
  public class BurnManager : IBurnManager
  {
    #region Event delegates
    
    public event BurningError BurningFailed;
    public event BurnProgress BurnProgressUpdate;

    #endregion

    private ILogger Logger;
    private List<Burner> AvailableDrives;
    private Burner CurrentDrive = null;
    private BurnStatus CurrentStatus = BurnStatus.Unknown;

    public BurnManager()
    {
      /*
      AvailableDrives = new List<Burner>(1);
      Logger = ServiceScope.Get<ILogger>();
      // Making sure the singleton constructor is called.
      DeviceHelper.Init();
      DeviceHelper.DeviceProgressUpdate += new DeviceHelper.DeviceProgress(DeviceHelper_ProgressUpdate);
      System.Windows.Forms.Application.ApplicationExit += new EventHandler(OnApplicationExit);

      Thread initDriveThread = new Thread(new ThreadStart(WorkerGetDrives));
      initDriveThread.IsBackground = true;      
      initDriveThread.Name = "BurnManager";
      initDriveThread.Priority = ThreadPriority.BelowNormal;
      initDriveThread.Start();*/
    }
    
    public void Dispose()
    {
      DeviceHelper.AbortOperations();      
    }

    #region public functions

    /// <summary>
    /// This will scan the bus system to query for available drives and create burn-drive objects
    /// </summary>
    /// <returns>whether there are burners found for this system</returns>
    public bool GetDrives()
    {
      bool result = false;
      AvailableDrives.Clear();
      // For now just use cdrtools to get drives, we could add WMI, Network-Burners etc. later.
      AvailableDrives = DeviceHelper.QueryBurners();
      
      if (AvailableDrives.Count < 1)
      {
        // maybe the system was not ready yet - try again...
        System.Threading.Thread.Sleep(1000);
        AvailableDrives.Clear();
        AvailableDrives = DeviceHelper.QueryBurners();
      }

      if (AvailableDrives.Count > 0)
      {
        result = true;
        foreach (Burner opticalDrive in AvailableDrives)
        {
          if (opticalDrive.DriveFeatures.WriteCDR && !opticalDrive.DriveFeatures.WriteDVDR)
            Logger.Info("BurnManager: Drive {0} available for CD burning. Supporting Dummymode: {1}, BurnFree: {2}, MaxWriteSpeed: {3}", opticalDrive.DeviceName, opticalDrive.DriveFeatures.AllowsDummyWrite.ToString(), opticalDrive.DriveFeatures.SupportsBurnFree.ToString(), opticalDrive.DriveFeatures.MaxWriteSpeed);
          else
            if (opticalDrive.DriveFeatures.WriteDVDR)
              Logger.Info("BurnManager: Drive {0} available for DVD burning. Supporting Dummymode: {1}, BurnFree: {2}, MaxWriteSpeed: {3}", opticalDrive.DeviceName, opticalDrive.DriveFeatures.AllowsDummyWrite.ToString(), opticalDrive.DriveFeatures.SupportsBurnFree.ToString(), opticalDrive.DriveFeatures.MaxWriteSpeed);
            else
              Logger.Info("BurnManager: Drive {0} available for reading only. Supporting CD: {1}, DVD: {2}, DVD-RAM: {3}, BD-ROM: {4}, MaxReadSpeed: {5}", opticalDrive.DeviceName, opticalDrive.DriveFeatures.ReadsCDR.ToString(), opticalDrive.DriveFeatures.ReadsDVDR.ToString(), opticalDrive.DriveFeatures.ReadsDVDRam.ToString(), opticalDrive.DriveFeatures.ReadsBRRom.ToString(), opticalDrive.DriveFeatures.MaxReadSpeed);
        }
        // for now choose the best drive - maybe pop up a first-time-config dialog later to select the preferred burner.
        SetPreferredDrive(AvailableDrives);
      }
      else      
        Logger.Info("BurnManager: No useable devices found");

      return result;
    }

    /// <summary>
    /// Retrieves the total size of all items including subdirectories
    /// </summary>
    /// <param name="aPathname">The path to check</param>
    /// <returns>Accumulated size of files in MB (neither MiB nor space used on disk)</returns>
    public int GetTotalMbForPath(string aPathname)
    {
      DirectoryInfo pathInfo = new DirectoryInfo(aPathname);
      return (int)(CalcTotalBytesOfDirInfo(pathInfo) / 1000000); //1048576); <-- Windows does use MB not MiB
    }

    /// <summary>
    /// Query this to find out e.g. whether to display the option to burn for a specific item.
    /// </summary>
    /// <param name="aProjectType">Does the item need a DVD or maybe an Audio-CD?</param>
    /// <param name="aCheckMediaStatus">Check the current inserted media as well and fail if not suitable</param>
    /// <param name="aBurnResult">For details on errors. Set to "Unknown" when calling</param>
    /// <returns></returns>
    public bool IsBurningPossible(ProjectType aProjectType, bool aCheckMediaStatus, out BurnResult aBurnResult)
    {
      // No burner found / set
      if (CurrentDrive == null)
      {
        aBurnResult = BurnResult.NoDriveAvailable;
        return false;
      }
      // E.g. CD-Burner does not support Data-DVD
      if (!MediaTypeSupport.CheckBurnerRequirements(aProjectType, CurrentDrive))
      {
        aBurnResult = BurnResult.UnsupportedInput;
        return false;
      }

      // Is the currently inserted Media ready for burning?
      if (aCheckMediaStatus)
      {
        // Check whether the correct type (CD,DVD) of media is inserted
        if (!MediaTypeSupport.CheckInsertedMediaType(aProjectType, CurrentDrive))
        {
          aBurnResult = BurnResult.WrongMediaType;
          return false;
        }
        // E.g. Does our DVD-R writer support the DVD+RW in tray
        if (!CheckInsertedMediaSupported(CurrentDrive))
        {
          aBurnResult = BurnResult.UnsupportedMedia;
          return false;
        }
        // Check if disk was already finalized, Auto-Blank Rewritable media
        if (!CheckInsertedMediaBlankStatus(CurrentDrive, true))
        {
          aBurnResult = BurnResult.NotEnoughSpace;
          return false;
        }
      }

      aBurnResult = BurnResult.Ready;
      return true;
    }

    public bool BlankDisk(bool aUseFastMode)
    {      
      string MyFastArg = aUseFastMode ? "blank=fast" : "blank=all";
      string MyBlankArgs = string.Format("dev={0} {1} {2}", CurrentDrive.BusId, MyFastArg, "-v -force");
      Logger.Info("BurnManager: Blanking inserted disk...");      
      DeviceHelper.ExecuteProcReturnStdOut("cdrecord.exe", MyBlankArgs, 900000); // assume 15 minutes max for blanking

      if (CurrentDrive.HasMedia) // to refresh the media status
        if (CurrentDrive.CurrentMediaInfo.DiskStatus == BlankStatus.empty)
          return true;

      return false;
    }

    public BurnResult BurnCdClone()
    {
      // readcd dev=3,0,0 speed=52 -clone -noerror f=/cygdrive/D/Temp/Burner/testcd.iso
      // cdda2wav dev=3,0,0 verbose-level=summary -gui -bulk -no-infofile track=1+2 /cygdrive/D/Temp/Burner/track
      ReportProgress(BurnStatus.Checking, 0);
      BurnResult result = BurnResult.Unknown;
      if (!IsBurningPossible(ProjectType.DataCD, false, out result))
      {
        ReportError(result, ProjectType.DataCD);
        return result;
      }
      string cacheFile = Path.Combine(Path.GetTempPath(), "MP2-temp.iso");
      string readCdArgs = "dev=" + CurrentDrive.BusId + " retries=128 -v -clone -nocorr -notrunc f=\"" + cacheFile + "\"";
      Logger.Info("BurnManager: Creating clone of inserted disk as {0}", cacheFile);
      ReportProgress(BurnStatus.Caching, 0);
      DeviceHelper.ExecuteProcReturnStdOut("readcd.exe", readCdArgs, 900000); // assume 15 minutes max for cloning
      
      // Swap source cd for the burning medium
      DeviceHelper.ExecuteProcReturnStdOut("cdrecord.exe", "dev=" + CurrentDrive.BusId + " -eject", 10000);
      System.Threading.Thread.Sleep(15000); // add input handler here

      if (!IsBurningPossible(ProjectType.DataCD, true, out result))
      {
        ReportError(result, ProjectType.DataCD);
        return result;
      }
      result = BurnIsoToDisk(cacheFile, true);
      if (result != BurnResult.Successful)
        ReportError(result, ProjectType.DataCD);
      return result;
    }

    public BurnResult BurnIsoFile(ProjectType aProjectType, string aIsoToBurn)
    {
      ReportProgress(BurnStatus.Checking, 0);
      BurnResult result = BurnResult.Unknown;
      if (!IsBurningPossible(aProjectType, true, out result))
      {
        ReportError(result, aProjectType);
        return result;
      }
      result = BurnIsoToDisk(aIsoToBurn, false);
      if (result != BurnResult.Successful)
        ReportError(result, aProjectType);
      return result;
    }

    public BurnResult BurnFolder(ProjectType aProjectType, string aPathToBurn)
    {
      ReportProgress(BurnStatus.Checking, 0);      
      BurnResult result = BurnResult.Unknown;
      if (!IsBurningPossible(aProjectType, true, out result))
      {
        ReportError(result, aProjectType);
        return result;
      }
      // make sure it always ends with a slash;
      string fullPathname = Path.GetDirectoryName(aPathToBurn + @"\") + @"\";

      if (!CheckRequiredSpaceForPath(aProjectType, fullPathname))
      {
        result = BurnResult.NotEnoughSpace;
        ReportError(result, aProjectType);
        return result;
      }

      string unixFilename = "\"" + fullPathname.Replace('\\', '/') + "\"";
      string cacheFile = Path.Combine(Path.GetTempPath(), "MP2-temp.iso");
      // build iso from path
      string IsoBuildArgs = DeviceHelper.GetCommonParamsForIsocreation("MP-II_" + DateTime.Now.ToShortDateString(), cacheFile);
      IsoBuildArgs += DeviceHelper.GetParamsByMedia(aProjectType);
      IsoBuildArgs += unixFilename;

      Logger.Info("BurnManager: Creating ISO of folder: {0} as {1}", fullPathname, cacheFile);
      DeviceHelper.ExecuteProcReturnStdOut("mkisofs.exe", IsoBuildArgs, 1800000); // assume 30 minutes max to create a 4,7 GB Iso

      result = BurnIsoToDisk(cacheFile, true);
      if (result != BurnResult.Successful)
        ReportError(result, aProjectType);
      return result;
    }

    #endregion

    #region private functions
    // internally used for all kind of prepared files.
    private BurnResult BurnIsoToDisk(string aIsoToBurn, bool aDeleteIsoOnSuccess)
    {
      List<string> MyCommandOutput = new List<string>(50);
      if (!aIsoToBurn.ToLowerInvariant().EndsWith(@".iso"))
        return BurnResult.UnsupportedInput;

      int isoSize = 0;
      try
      {
        FileInfo fileInfo = new FileInfo(aIsoToBurn);
        isoSize = (int)(fileInfo.Length / 1048576); // GetMB
      }
      catch (Exception)
      {
        return BurnResult.ErrorConverting;
      }
      if (!CheckInsertedMediaCapacity(CurrentDrive, isoSize))
      {
        Logger.Warn("BurnManager: Could not burn ISO file {0} because it doesn't fit on the medium", aIsoToBurn);
        return BurnResult.NotEnoughSpace;
      }
      string unixFilename = aIsoToBurn.Replace('\\', '/');
      string IsoArgs = DeviceHelper.GetCommonParamsForDevice(CurrentDrive) + " \"" + unixFilename + "\"";
      
      MyCommandOutput = DeviceHelper.ExecuteProcReturnStdOut("cdrecord.exe", IsoArgs, 3600000);

      if (CurrentStatus == BurnStatus.Finished && !MyCommandOutput.Contains(@"A write error occured."))
      {
        // if unsuccessful keep the ISO for retrying / debugging
        if (aDeleteIsoOnSuccess)
          CleanUpCachedFiles(unixFilename);

        return BurnResult.Successful;
      }
      else
        return BurnResult.ErrorBurning;
    }

    private void CleanUpCachedFiles(string unixFilename)
    {
      try
      {
        Logger.Debug("BurnManager: Deleting cached file {0}", unixFilename);
        File.Delete(unixFilename);        
      }
      catch (Exception ex)
      {
        Logger.Warn("BurnManager: Could not delete cached file {0}, Error: {1}", unixFilename, ex.Message);
      }
    }


    /// <summary>
    /// Checks whether the needed media can be handled
    /// </summary>
    /// <param name="aSelectedBurner">the burner which is going to be checked for support of the current media</param>
    /// <returns>whether the burner can handle the inserted media type</returns>
    private bool CheckInsertedMediaSupported(Burner aSelectedBurner)
    {
      if (aSelectedBurner == null)
        return false;

      // this will trigger a refresh of the current media as well
      if (!aSelectedBurner.HasMedia)
        return false;
      else
      {
        return aSelectedBurner.MediaFeatures.IsMediaTypeSupported(aSelectedBurner.CurrentMediaInfo.CurrentMediaType);
      }
    }

    private bool CheckInsertedMediaBlankStatus(Burner aSelectedBurner, bool aBlankRewriteable)
    {
      if (aSelectedBurner.CurrentMediaInfo.DiskStatus == BlankStatus.complete)
      {
        if (aBlankRewriteable)
        {
          if (aSelectedBurner.CurrentMediaInfo.IsErasable)
          {
            // DVD+RW are overwritten directly
            if (aSelectedBurner.CurrentMediaInfo.CurrentMediaType == MediaType.DVDplusRW)
              return true;

            // Blank disk now!
            return BlankDisk(true);            
          }
        }
        // did not blank so do not use the medium
        return false;
      }
      else
        return true;
    }

    private bool CheckInsertedMediaCapacity(Burner aSelectedBurner, int aIsoSize)
    {
      int currentSpace = (int)(aSelectedBurner.CurrentMediaInfo.Size / 1024);
      int currentSpaceGuess = MediaTypeSupport.GetMediaSizeMbByType(aSelectedBurner.CurrentMediaInfo.CurrentMediaType);
      // first try the "true" disk size
      if (currentSpace < aIsoSize)
      {
        return (currentSpaceGuess > aIsoSize);
      }

      return true;
    }

    private bool CheckRequiredSpaceForPath(ProjectType aProjectType, string fullPathname)
    {
      int availableSpace = MediaTypeSupport.GetMaxMediaSizeMbByProjectType(aProjectType, CurrentDrive);
      int neededSpace = GetTotalMbForPath(fullPathname);

      Logger.Debug("BurnManager: Project {0} of {1} needs a media with an estimated size of at least {2} MB - available: {3} MB", aProjectType.ToString(), fullPathname, Convert.ToString(neededSpace), Convert.ToString(availableSpace));
      return (availableSpace >= neededSpace);
    }

    // Caution recursively calls itself - may last some seconds
    private long CalcTotalBytesOfDirInfo(DirectoryInfo aPathInfo)
    {
      long Size = 0;
      try
      {
        FileInfo[] recursiveFi = aPathInfo.GetFiles();
        foreach (FileInfo fi in recursiveFi)
        {
          Size += fi.Length;
        }
      }
      catch (Exception ex)
      {
        Logger.Debug("BurnManager: Checking file size failed for {0} - {1}", aPathInfo.FullName, ex.Message);
      }

      try
      {
        // Add subdirectory sizes.
        DirectoryInfo[] subInfo = aPathInfo.GetDirectories();
        foreach (DirectoryInfo di in subInfo)
        {
          Size += CalcTotalBytesOfDirInfo(di);
        }
      }
      catch (Exception)
      {
        //Logger.Debug("BurnManager: Checking subdirectory size failed for {0} - {1}", aPathInfo.FullName, ex2.Message);
      }

      return (Size);
    }

    private void SetPreferredDrive(List<Burner> AvailableDrives)
    {
      foreach (Burner MyDrive in AvailableDrives)
      {
        if (CurrentDrive == null)
        {
          if (MyDrive.DriveFeatures.WriteCDR || MyDrive.DriveFeatures.WriteDVDR)
            CurrentDrive = MyDrive;
        }
        else
          // Prefer new/faster drives over older drives
          if (MyDrive.DriveFeatures.MaxWriteSpeedInt > CurrentDrive.DriveFeatures.MaxWriteSpeedInt)
          {
            // Prefer more flexible drives supporting DVD-RAM over faster drives
            if (!MyDrive.DriveFeatures.WriteDVDRam && CurrentDrive.DriveFeatures.WriteDVDRam)
              continue;

            CurrentDrive = MyDrive;
          }
      }
      if (CurrentDrive != null)
      {
        Logger.Info("BurnManager: Drive {0}-{1} will be used as preferred burner.", CurrentDrive.DeviceVendor, CurrentDrive.DeviceName);
        if (CurrentDrive.HasMedia)
          Logger.Info("BurnManager: The drive is equipped with a {0} medium ({1}).", CurrentDrive.CurrentMediaInfo.HumanMediaString, CurrentDrive.CurrentMediaInfo.DiskStatus.ToString());
      }
    }

    // We need this because the BurnManager implements the interface event but does not do device output parsing
    private void DeviceHelper_ProgressUpdate(BurnStatus eBurnStatus, int ePercentage)
    {
      ReportProgress(eBurnStatus, ePercentage);
    }

    // Have a common function so we do not need to check for registered events everywhere
    private void ReportProgress(BurnStatus aBurnStatus, int aPercentage)
    {
      CurrentStatus = aBurnStatus;
      if (BurnProgressUpdate != null)
        BurnProgressUpdate(aBurnStatus, aPercentage);
    }

    private void ReportError(BurnResult aBurnResult, ProjectType aProjectType)
    {
      if (BurningFailed != null)
        BurningFailed(aBurnResult, aProjectType);
    }

    private void OnApplicationExit(object sender, EventArgs e)
    {
      this.Dispose();
    }

    // Since gathering device info is not time critical perform it by a background thread for now
    private void WorkerGetDrives()
    {
      // Get MP some time to start up quickly on single core CPUs
      System.Threading.Thread.Sleep(8000);
      GetDrives();

      System.Windows.Forms.Application.DoEvents();
      System.Threading.Thread.Sleep(2000);
      // Do a test burn which has nothing to do at this place - do not worry; I was just lazy :P
      // BurnCdClone();
      // BurnIsoFile(ProjectType.IsoDVD, @"E:\Dateien\Quellen\MP2\MediaPortal\bin\x86\Debug\Burner\test.iso");
      // BurnFolder(ProjectType.Autoselect, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
    }

    #endregion
  }
}
