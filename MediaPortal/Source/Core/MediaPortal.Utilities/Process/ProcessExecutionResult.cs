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

namespace MediaPortal.Utilities.Process
{
  /// <summary>
  /// Represents the result of running an external process
  /// </summary>
  public class ProcessExecutionResult
  {
    /// <summary>
    /// Convenience method to check the <see cref="ExitCode"/> of a process
    /// Returns <c>true</c> if the <see cref="ExitCode"/> is 0; otherwise false
    /// </summary>
    public bool Success { get { return ExitCode == 0; } }

    /// <summary>
    /// Contains the ExitCode of the process
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Contains the StandardOutput of the process
    /// </summary>
    public String StandardOutput { get; set; }

    /// <summary>
    /// Contains the StandardError output of the process
    /// </summary>
    public String StandardError { get; set; }
  }
}
