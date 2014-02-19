#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.IO;
using System.Linq;

namespace MediaPortal.Common.Services.ResourceAccess.Settings
{
  /// <summary>
  /// <see cref="AvailableDriveLettersSettings"/> is a helper class that can be used to query available drive letters. The class is serializable,
  /// so the Client can load this information from Server.
  /// </summary>
  public class AvailableDriveLettersSettings
  {
    public AvailableDriveLettersSettings()
    {
      AvailableDriveLetters = Enumerable.Range('A', 26).Select(c => (char) c).Except(Directory.GetLogicalDrives().Select(d => d[0])).ToArray();
    }

    /// <summary>
    /// Gets a list of available drive letters on current system.
    /// </summary>
    public char[] AvailableDriveLetters { get; set; }
  }
}