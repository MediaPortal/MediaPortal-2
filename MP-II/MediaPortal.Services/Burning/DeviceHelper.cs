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
using System.Diagnostics;
using System.IO;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.DeviceManager;


namespace MediaPortal.Services.Burning
{
  public class DeviceHelper
  {
    #region events

    public delegate void DeviceProgress(BurnStatus eBurnStatus, int ePercentage);
    public static event DeviceProgress DeviceProgressUpdate;

    #endregion

    #region variables

    private static DeviceHelper fDeviceHelper;
    private static ILogger Logger;
    private static List<string> StdOutList;
    private static int fIsoSizeMB = 0;

    #endregion

    #region static methods

    /// <summary>
    /// Init class.
    /// </summary>
    public static void Init()
    {
      if (fDeviceHelper == null)
        fDeviceHelper = new DeviceHelper();
    }

    #region fields

    /// <summary>
    /// This points to the path were cdrtools are located
    /// </summary>
    public static string BurnToolPath
    {
      get { return Path.Combine(System.Windows.Forms.Application.StartupPath, @"Burner"); }
    }

    public static int IsoSizeMB
    {
      get { return fIsoSizeMB; }
    }

    #endregion

    /// <summary>
    /// Build all medium independent arguments to apply for each burning action
    /// </summary>
    /// <param name="aDevice">The device to use (may need some specific params)</param>
    /// <returns>All trimmed arguments concatenated and ready to pass them to cdrecord</returns>
    public static string GetCommonParamsForDevice(Burner aDevice)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(" dev=");
      sb.Append(aDevice.BusId);            // how to address the device
      sb.Append(" gracetime=2");           // wait this many seconds until finally the process is started
      sb.Append(" fs=8m");                 // set fifo buffer to 8 MB (should be at least the size of the drive's cache)
      if (aDevice.DriveFeatures.SupportsBurnFree)
        sb.Append(" driveropts=burnfree"); // enable Buffer-Underrun-Technology
      sb.Append(" -v");                    // be a little verbose
      sb.Append(" -overburn");             // allow overburning just in case we calculate wrong by a few KiB
      sb.Append(" -dao");                  // overburning usually requires disc-at-once mode                                                             
      sb.Append(" -eject");                // Open Tray when finished
      // sb.Append(" -clone");                // Write disk in clone mode
      // sb.Append(" -dummy");                // only simulate for debugging purposes

      return sb.ToString();
    }

    /// <summary>
    /// Build all common params needed by mkisofs
    /// </summary>
    /// <param name="aDiskLabel">How should the disk be named</param>
    /// <param name="aTargetFilename">Where should the ISO be stored</param>
    /// <returns>Trimmed, concatenated params</returns>
    public static string GetCommonParamsForIsocreation(string aDiskLabel, string aTargetFilename)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(" -V ");
      sb.Append(aDiskLabel);      
      sb.Append(" -l");                // allow long filenames
      sb.Append(" -allow-lowercase");  // ISO9660 would not allow lower chars by default
      sb.Append(" -relaxed-filenames");
      sb.Append(" -input-charset=");
      sb.Append(Encoding.GetEncoding(0).BodyName);  // get the current system encoding
      sb.Append(" -output-charset=");
      sb.Append(Encoding.GetEncoding(0).BodyName);
      sb.Append(" -preparer \"MediaPortal-II\"");
      sb.Append(" -publisher \"");
      sb.Append(Environment.UserName);
      sb.Append("\" -o \"");
      sb.Append(aTargetFilename);
      sb.Append("\" ");

      return sb.ToString();
    }

    /// <summary>
    /// Build all media related params needed by mkisofs
    /// </summary>
    /// <param name="aProjectType">What should be burned</param>
    /// <returns>Trimmed, concatenated params</returns>
    public static string GetParamsByMedia(ProjectType aProjectType)
    {
      StringBuilder sb = new StringBuilder();

      switch (aProjectType)
      {
        case ProjectType.Autoselect:     // UDF will suit 99% of all cases
          goto case ProjectType.LargeDataDVD;
        case ProjectType.DataCD:
          sb.Append(" -iso-level 4");    // use most recent deprecated standard
          sb.Append(" -J -joliet-long"); // add Joliet extension for Windows
          sb.Append(" -R ");             // add Rock Ridge extension for Linux < 2.6
          break;
        case ProjectType.AudioCD:        // these are directly written do disk
          break;
        case ProjectType.PhotoCD:        // need to read about that..          
          break;
        case ProjectType.IsoCD:          // already done
          break;
        case ProjectType.DataDVD:
          goto case ProjectType.LargeDataDVD;
        case ProjectType.VideoDVD:       // will use DataDVD from dvdauthor here
          goto case ProjectType.LargeDataDVD;
        case ProjectType.IsoDVD:         // already done
          break;
        case ProjectType.LargeDataDVD:
          sb.Append(" -UDF ");           // create UDF file system (not supported by Win95 but does allow for files > 2 GB)
          break;
        case ProjectType.LargeIsoDVD:    // already done     
          break;
        default:
          break;          
      }

      return sb.ToString();
    }

    /// <summary>
    /// Scan the bus for suitable burning devices
    /// </summary>
    /// <returns>All burning enabled drives</returns>
    public static List<Burner> QueryBurners()
    {
      if (fDeviceHelper != null)
      {
        return fDeviceHelper.QueryForBurners();
      }
      else
        return null;
    }

    /// <summary>
    /// Kill external processes inmediately e.g. for proper shutdown.
    /// </summary>
    public static void AbortOperations()
    {
      List<string> propableProcs = new List<string>();
      propableProcs.Add(@"cdrecord");
      propableProcs.Add(@"mkisofs");

      foreach (string myProcessName in propableProcs)
      {
        Process[] abortProcs = Process.GetProcessesByName(myProcessName);
        foreach (Process termProc in abortProcs)
        {
          try
          {
            Logger.Error("Devicehelper: Killing process: {0}", termProc.ProcessName);
            termProc.Kill();
          }
          catch (Exception ex)
          {
            Logger.Error("Devicehelper: Could not kill {0} - {1}", termProc.ProcessName, ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// Executes commandline processes and parses their output
    /// </summary>
    /// <param name="aAppName">The burn tool to launch (e.g. cdrecord.exe or mkisofs.exe)</param>
    /// <param name="aArguments">The arguments to supply for the given process</param>
    /// <param name="aExpectedTimeoutMs">How long the function will wait until the tool's execution will be aborted</param>
    /// <returns>A list containing the redirected StdOut line by line</returns>
    public static List<string> ExecuteProcReturnStdOut(string aAppName, string aArguments, int aExpectedTimeoutMs)
    {
      lock (fDeviceHelper)
      {
        StdOutList.Clear();

        Process CdrProc = new Process();
        ProcessStartInfo ProcOptions = new ProcessStartInfo(Path.Combine(BurnToolPath, aAppName), aArguments);

        ProcOptions.UseShellExecute = false;                                       // Important for WorkingDirectory behaviour
        ProcOptions.RedirectStandardError = true;                                  // .NET bug? Some stdout reader abort to early without that!
        ProcOptions.RedirectStandardOutput = true;                                 // The precious data we're after
        ProcOptions.StandardOutputEncoding = Encoding.GetEncoding("ISO-8859-1");  // the output contains "Umlaute", etc.
        ProcOptions.StandardErrorEncoding = Encoding.GetEncoding("ISO-8859-1");
        ProcOptions.WorkingDirectory = BurnToolPath;                               // set the dir because the binary might depend on cygwin.dll
        ProcOptions.CreateNoWindow = true;                                         // Do not spawn a "Dos-Box"      
        ProcOptions.ErrorDialog = false;                                           // Do not open an error box on failure        

        CdrProc.OutputDataReceived += new DataReceivedEventHandler(StdOutDataReceived);
        CdrProc.ErrorDataReceived += new DataReceivedEventHandler(StdErrDataReceived);
        CdrProc.EnableRaisingEvents = true;                                        // We want to know when and why the process died        
        CdrProc.StartInfo = ProcOptions;
        if (File.Exists(ProcOptions.FileName))
        {
          try
          {
            CdrProc.Start();
            CdrProc.BeginErrorReadLine();
            CdrProc.BeginOutputReadLine();
            try
            {
              //CdrProc.PriorityClass = ProcessPriorityClass.BelowNormal;            // Execute all processes in the background so movies, etc stay fluent
            }
            catch (Exception ex2)
            {
              Logger.Error("Devicehelper: Error setting process priority for {0}: {1}", aAppName, ex2.Message);
            }
            // wait this many seconds until crdtools has to be finished
            CdrProc.WaitForExit(aExpectedTimeoutMs);
            if (CdrProc.HasExited && CdrProc.ExitCode != 0)
              ProcessErrorHandler(aAppName, aArguments, CdrProc.ExitCode);              
          }
          catch (Exception ex)
          {
            Logger.Error("Devicehelper: Error executing {0}: {1}", ex.Message);
          }
        }
        else
          Logger.Warn("Devicehelper: Could not start {0} because it doesn't exist!", ProcOptions.FileName);

        return StdOutList;
      }
    }

    private static void ProcessErrorHandler(string aAppName, string aArguments, int aExitcode)
    {
      switch (aAppName)
      {
        case "cdrecord.exe":
          if (!aArguments.Contains(@"-minfo"))
            Logger.Warn("Devicehelper: {0} did not exit properly with arguments: {1}, exitcode: {2}", aAppName, aArguments, aExitcode);
          break;
        case "mkisofs.exe":
          if (aExitcode == 253)
            Logger.Error("Devicehelper: ISO creation failed. Possible error: The source files did change.");
          else
            Logger.Warn("Devicehelper: {0} did not exit properly with arguments: {1}, exitcode: {2}", aAppName, aArguments, aExitcode);
          break;
      }
    }
    
    #region output handler

    private static void StdErrDataReceived(object sendingProc, DataReceivedEventArgs errLine)
    {
      if (!string.IsNullOrEmpty(errLine.Data))
      {
        if (errLine.Data.Contains(@"estimate finish"))
        {
          string percentage;
          double convert = 0;
          try
          {
            percentage = errLine.Data.Remove(6); //  10.81% done, estimate finish Tue Nov  6 03:23:12 2007
            convert = Convert.ToDouble(percentage, System.Globalization.CultureInfo.InvariantCulture);
          }
          catch (Exception)
          {
          }
          fDeviceHelper.ReportProgress(BurnStatus.Converting, (int)convert);
        }
        else
          if (errLine.Data.Contains(@"extents written"))
          {
            int pos = errLine.Data.IndexOf('(') + 1;
            if (pos > 0)
            {
              string isoSize = errLine.Data.Substring(pos);
              pos = isoSize.IndexOf("MB");
              if (pos > 0)
              {
                isoSize = isoSize.Remove(pos).Trim();
                fIsoSizeMB = Convert.ToInt16(isoSize);
                Logger.Info("Devicehelper: Created ISO has a size of {0} MB", isoSize);
              }
            }
          }
          else
            if (errLine.Data.Contains(@"times empty"))
            {
              int pos = errLine.Data.IndexOf("fifo");
              if (pos >= 0)
              {
                string bufferMsg = errLine.Data.Substring(pos);
                Logger.Info("Devicehelper: Buffer status: {0}", bufferMsg);
              }
            }
            //else // <-- activate for all debug output
              //Logger.Debug("Devicehelper: StdErr received unclassified message: {0}", errLine.Data);
      }
    }

    private static void StdOutDataReceived(object sendingProc, DataReceivedEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.Data))
      {
        StdOutList.Add(e.Data);

        if (e.Data.Contains("written"))
        { // "Track 02:    1 of  451 MB written (fifo  94%) [buf  60%]   0.1x."
          // "Writing  time:  123.891s"
          // "Average write speed   2.3x."
          // "Min drive buffer fill was 96%"
          int pos = e.Data.IndexOf(':');
          if (pos > 0)
          {
            string progress = e.Data.Substring(pos + 1);
            int MyIsoSize = fIsoSizeMB;

            pos = progress.IndexOf("of");
            if (pos > 0)
            {
              if (MyIsoSize < 1)
              {
                // property not set during iso creation - use cdrecord's output
                MyIsoSize = Convert.ToInt32(progress.Substring(pos + 2, 5).Trim());
                fIsoSizeMB = MyIsoSize;
              }

              if (MyIsoSize > 0)
              {
                progress = progress.Remove(pos).Trim();
                int percentage = (int)((Convert.ToInt16(progress) * 100) / MyIsoSize);
                fDeviceHelper.ReportProgress(BurnStatus.Burning, percentage);
              }
            }
          }
        }
        else
          if (e.Data.Contains("Average write speed"))
            Logger.Info("DeviceHelper: {0}", e.Data);
          else
            if (e.Data.Contains("Fixating.."))
              fDeviceHelper.ReportProgress(BurnStatus.LeadOut, 0);
            else
              // Fixating time:    2.015s
              if (e.Data.Contains("Fixating time"))
                fDeviceHelper.ReportProgress(BurnStatus.Finished, 0);
              //else
                //Logger.Debug("Devicehelper: StdOut received unclassified message: {0}", e.Data);

      }
    }

    #endregion

    #endregion

    #region constructor

    public DeviceHelper()
    {
      Logger = ServiceScope.Get<ILogger>();
      StdOutList = new List<string>(80);
    }

    #endregion

    #region private functions

    private List<Burner> QueryForBurners()
    {
      try
      {
        List<string> DeviceInfo = new List<string>(74);
        List<string> FoundDeviceIDs = new List<string>(2);
        List<Burner> FoundDrives = new List<Burner>(1);
        DeviceInfo = ExecuteProcReturnStdOut("cdrecord.exe", "-scanbus", 45000);

        // fetch all optical drives
        FoundDeviceIDs = ParsePossibleDevices(DeviceInfo);

        foreach (string devstr in FoundDeviceIDs)
        {
          FoundDrives.Add(new Burner(devstr));
          // let pending job finish
          System.Windows.Forms.Application.DoEvents();
        }

        return FoundDrives;
      }
      catch (Exception)
      {
        Logger.Error("BurnManager: Error scanning SCSI bus for devices");
        return null;
      }
    }

    private List<string> ParsePossibleDevices(List<string> aOutPutData)
    {
      List<string> devLines = new List<string>(5);
      
      foreach (string scanLine in aOutPutData)
      {
        if (scanLine.EndsWith(@"Removable CD-ROM"))
          devLines.Add(scanLine.Substring(1,5));
      }

      return devLines;
    }

    // Have a common function so we do not need to check for registered events everywhere
    private void ReportProgress(BurnStatus aBurnStatus, int aPercentage)
    {
      if (DeviceProgressUpdate != null)
        DeviceProgressUpdate(aBurnStatus, aPercentage);
    }

    #endregion
  }
}
