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
using System.Text;
using System.Runtime.InteropServices;

namespace MediaPortal.Utilities.Win32
{
  /// <summary>
  /// Contains Environment Specific Information
  /// </summary>
  class EnvironmentUtils
  {
    #region Enums
    public enum OsProductType
    {
      Workstation = 1,
      Server = 2
    }

    public enum OperatingSystem
    {
      Unknown,
      Windows95,
      Windows98,
      Windows98SE,
      WindowsMe,
      WindowsNT351,
      WindowsNT,
      Windows2000,
      Windows2003,
      WindowsXP,
      Vista,
      Windows2008
    }

    public enum OperatingSystemProductType
    {
      Unknown,
      Workstation,    // NT 4 Workstation
      Server,         // NT 4 Server
      Home,           // XP Home, Vista Home & Basic
      Professional,   // XP & Vista Professional
      Datacenter,     // 2000, 2003, 2008 Datacenter Server
      Advanced,       // 2000 Advanced Server
      Standard,       // 2000, 2003, 2008 Standard Server
      Enterprise,     // 2000, 2003, 2008 Enterprise Server
      Web,            // 2003 Web Edition Server
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets the full version of the operating system running on this computer.
    /// </summary>
    public static string OSVersion
    {
      get
      {
        return System.Environment.OSVersion.Version.ToString();
      }
    }

    /// <summary>
    /// Gets the major version of the operating system running on this computer.
    /// </summary>
    public static int OSMajorVersion
    {
      get
      {
        return System.Environment.OSVersion.Version.Major;
      }
    }

    /// <summary>
    /// Gets the minor version of the operating system running on this computer.
    /// </summary>
    public static int OSMinorVersion
    {
      get
      {
        return System.Environment.OSVersion.Version.Minor;
      }
    }

    /// <summary>
    /// Gets the build version of the operating system running on this computer.
    /// </summary>
    public static int OSBuildVersion
    {
      get
      {
        return System.Environment.OSVersion.Version.Build;
      }
    }

    /// <summary>
    /// Gets the revision version of the operating system running on this computer.
    /// </summary>
    public static int OSRevisionVersion
    {
      get
      {
        return System.Environment.OSVersion.Version.Revision;
      }
    }
    #endregion

    #region Methods

    /// <summary>
    /// Returns the the operating system running on this computer.
    /// </summary>
    /// <returns>the Operating System Used</returns>
    public static OperatingSystem GetOperatingSystem()
    {

      Win32API.OSVERSIONINFOEX osVersionInfo = new Win32API.OSVERSIONINFOEX();
      osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(Win32API.OSVERSIONINFOEX));
      Win32API.GetVersionEx(ref osVersionInfo);

      System.OperatingSystem osInfo = System.Environment.OSVersion;

      switch (osInfo.Platform)
      {
        case PlatformID.Win32Windows:
          {
            switch (osInfo.Version.Minor)
            {
              case 0:
                {
                  return OperatingSystem.Windows95;
                }

              case 10:
                {
                  if (osInfo.Version.Revision.ToString() == "2222A")
                  {
                    return OperatingSystem.Windows98SE;
                  }
                  else
                  {
                    return OperatingSystem.Windows98;
                  }
                }

              case 90:
                {
                  return OperatingSystem.WindowsMe;
                }
            }
            break;
          }

        case PlatformID.Win32NT:
          {
            switch (osInfo.Version.Major)
            {
              case 3:
                {
                  return OperatingSystem.WindowsNT351;
                }

              case 4:
                {
                  return OperatingSystem.WindowsNT;
                }

              case 5:
                {
                  if (osInfo.Version.Minor == 0)
                  {
                    return OperatingSystem.Windows2000;
                  }
                  else if (osInfo.Version.Minor == 1)
                  {
                    return OperatingSystem.WindowsXP;
                  }
                  else if (osInfo.Version.Minor == 2)
                  {
                    return OperatingSystem.Windows2003;
                  }
                  break;
                }

              case 6:
                {
                  if (osVersionInfo.wSuiteMask == Win32API.VER_NT_WORKSTATION)
                    return OperatingSystem.Vista;
                  else
                    return OperatingSystem.Windows2008;
                }
            }
            break;
          }
      }
      return OperatingSystem.Unknown;
    }

    /// <summary>
    /// Returns the product type of the operating system running on this computer.
    /// </summary>
    /// <returns>The OperatingSystemProductType enum</returns>
    public static OperatingSystemProductType GetOperatingSystemProductType()
    {
      Win32API.OSVERSIONINFOEX osVersionInfo = new Win32API.OSVERSIONINFOEX();
      System.OperatingSystem osInfo = System.Environment.OSVersion;

      osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(Win32API.OSVERSIONINFOEX));

      if (!Win32API.GetVersionEx(ref osVersionInfo))
      {
        return OperatingSystemProductType.Unknown;
      }

      if (osInfo.Version.Major == 4)
      {
        if (osVersionInfo.wProductType == Win32API.VER_NT_WORKSTATION)
        {
          // Windows NT 4.0 Workstation
          return OperatingSystemProductType.Workstation;
        }
        else if (osVersionInfo.wProductType == Win32API.VER_NT_SERVER)
        {
          // Windows NT 4.0 Server
          return OperatingSystemProductType.Server;
        }
        else
        {
          return OperatingSystemProductType.Unknown;
        }
      }
      else if (osInfo.Version.Major == 5)
      {
        if (osVersionInfo.wProductType == Win32API.VER_NT_WORKSTATION)
        {
          if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_PERSONAL) == Win32API.VER_SUITE_PERSONAL)
          {
            // Windows XP Home Edition
            return OperatingSystemProductType.Home;
          }
          else
          {
            // Windows XP / Windows 2000 Professional
            return OperatingSystemProductType.Professional;
          }
        }
        else if (osVersionInfo.wProductType == Win32API.VER_NT_SERVER)
        {
          if (osInfo.Version.Minor == 0)
          {
            if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_DATACENTER) == Win32API.VER_SUITE_DATACENTER)
            {
              // Windows 2000 Datacenter Server
              return OperatingSystemProductType.Datacenter;
            }
            else if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_ENTERPRISE) == Win32API.VER_SUITE_ENTERPRISE)
            {
              // Windows 2000 Advanced Server
              return OperatingSystemProductType.Advanced;
            }
            else
            {
              // Windows 2000 Server
              return OperatingSystemProductType.Standard;
            }
          }
          else
          {
            if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_DATACENTER) == Win32API.VER_SUITE_DATACENTER)
            {
              // Windows Server 2003 Datacenter Edition
              return OperatingSystemProductType.Datacenter;
            }
            else if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_ENTERPRISE) == Win32API.VER_SUITE_ENTERPRISE)
            {
              // Windows Server 2003 Enterprise Edition
              return OperatingSystemProductType.Enterprise;
            }
            else if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_BLADE) == Win32API.VER_SUITE_BLADE)
            {
              // Windows Server 2003 Web Edition
              return OperatingSystemProductType.Web;
            }
            else
            {
              // Windows Server 2003 Standard Edition
              return OperatingSystemProductType.Standard;
            }
          }
        }
      }
      else if (osInfo.Version.Major == 6)
      {
        if (osVersionInfo.wProductType == Win32API.VER_NT_WORKSTATION)
        {
          if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_PERSONAL) == Win32API.VER_SUITE_PERSONAL)
          {
            // Vista Home or Basic Edition
            return OperatingSystemProductType.Home;
          }
          else
          {
            // Vista Professional
            return OperatingSystemProductType.Professional;
          }
        }
        else if (osVersionInfo.wProductType == Win32API.VER_NT_SERVER)
        {
          if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_DATACENTER) == Win32API.VER_SUITE_DATACENTER)
          {
            // Windows Server 2008 Datacenter Edition
            return OperatingSystemProductType.Datacenter;
          }
          else if ((osVersionInfo.wSuiteMask & Win32API.VER_SUITE_ENTERPRISE) == Win32API.VER_SUITE_ENTERPRISE)
          {
            // Windows Server 2008 Enterprise Edition
            return OperatingSystemProductType.Enterprise;
          }
          else
          {
            // Windows Server 2008 Standard Edition
            return OperatingSystemProductType.Standard;
          }
        }
      }

      return OperatingSystemProductType.Unknown;
    }


    /// <summary>
    /// Returns the service pack information of the operating system running on this computer.
    /// </summary>
    /// <returns>A string containing the the operating system service pack information.</returns>
    public static string GetOSServicePack()
    {
      Win32API.OSVERSIONINFOEX osVersionInfo = new Win32API.OSVERSIONINFOEX();

      osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof(Win32API.OSVERSIONINFOEX));

      if (!Win32API.GetVersionEx(ref osVersionInfo))
      {
        return "";
      }
      else
      {
        return " " + osVersionInfo.szCSDVersion;
      }
    }

    #endregion
  }
}
