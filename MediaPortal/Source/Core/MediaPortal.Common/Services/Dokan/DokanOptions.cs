#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Dokan mount options used to describe dokan device behaviour.
  /// </summary>
  [Flags]
  public enum DokanOptions : long
  {
    /// <summary>Enable ouput debug message</summary>
    DebugMode = 1,

    /// <summary>Enable ouput debug message to stderr</summary>
    StderrOutput = 2,

    /// <summary>Use alternate stream</summary>
    AltStream = 4,

    /// <summary>Enable mount drive as write-protected.</summary>
    WriteProtection = 8,

    /// <summary>Use network drive - Dokan network provider need to be installed.</summary>
    NetworkDrive = 16,

    /// <summary>Use removable drive</summary>
    RemovableDrive = 32,

    /// <summary>Use mount manager</summary>
    MountManager = 64,

    /// <summary>Mount the drive on current session only</summary>
    CurrentSession = 128,

    /// <summary>Fixed Driver</summary>
    FixedDrive = 0,
  }
}
