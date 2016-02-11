#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Management;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.AspNetWebApi.ServerInformation
{
  /// <summary>
  /// Collects all important Server information like Disk usage, Cpu usage and Ram usage
  /// </summary>
  public class ServerInformation
  {
    #region Public Fields

    public List<DiskInformation> Drives { get; set; }
    public int CpuUsage { get; set; }
    public RamInformation Ram { get; set; }

    #endregion

    #region Constructor

    public ServerInformation()
    {
      Drives = GetDiskInformation();
      CpuUsage = GetCpuUsage();
      Ram = GetRamInformation();
    }

    #endregion

    #region Private methods

    private List<DiskInformation> GetDiskInformation()
    {
      DriveInfo[] allDrives = DriveInfo.GetDrives();

      return allDrives.Where(drive => drive.IsReady).Select(drive => new DiskInformation
      {
        Name = drive.VolumeLabel,
        Letter = drive.Name,
        FileSystem = drive.DriveFormat,
        TotalFreeSpace = drive.TotalFreeSpace,
        TotalSize = drive.TotalSize
      }).ToList();
    }

    private int GetCpuUsage()
    {
      try
      {
        //Get CPU usage values using a WMI query
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor");
        var cpuTimes = searcher.Get()
          .Cast<ManagementObject>()
          .Select(mo => new
          {
            Name = mo["Name"],
            Usage = mo["PercentProcessorTime"]
          })
          .ToList();

        var query = cpuTimes.Where(x => x.Name.ToString() == "_Total").Select(x => x.Usage);
        int cpuUsage;
        bool parseResult = int.TryParse(query.SingleOrDefault()?.ToString(), out cpuUsage);

        return parseResult ? cpuUsage : 0;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to retrieve CPU usage", ex);
        return 0;
      }
    }

    private RamInformation GetRamInformation()
    {
      RamInformation ramInformation = new RamInformation();

      try
      {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");

        double freeRam = 0;
        double totalRam = 0;

        foreach (var o in searcher.Get())
        {
          var queryObj = (ManagementObject)o;

          if (queryObj["FreePhysicalMemory"] != null)
            double.TryParse(queryObj["FreePhysicalMemory"].ToString(), out freeRam);

          if (queryObj["TotalVisibleMemorySize"] != null)
            double.TryParse(queryObj["TotalVisibleMemorySize"].ToString(), out totalRam);
        }

        ramInformation.Free = freeRam;
        ramInformation.Total = totalRam;
        ramInformation.Used = Math.Round(((totalRam - freeRam) / totalRam * 100), 2);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Failed to retrieve Ram information", ex);
      }

      return ramInformation;
    }

    #endregion
  }
}
