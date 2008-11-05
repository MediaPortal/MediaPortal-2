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

namespace MediaPortal.Core.DeviceManager
{
  public enum MediaType
  {
    None = 0,
    /// <summary>
    /// All types of read-only mediums
    /// </summary>
    ReadOnly = 1,
    /// <summary>
    /// CD Writeable
    /// </summary>
    CDR = 2,
    /// <summary>
    /// CD Re-Writeable
    /// </summary>
    CDRW = 3,
    /// <summary>
    /// DVD+R
    /// </summary>
    DVDplusR = 4,
    /// <summary>
    /// DVD-R
    /// </summary>
    DVDminusR = 5,
    /// <summary>
    /// DVD+RW
    /// </summary>
    DVDplusRW = 6,
    /// <summary>
    /// DVD-RW
    /// </summary>
    DVDminusRW = 7,
    /// <summary>
    /// Double Layer DVD+R
    /// </summary>
    DlDVDplusR = 8,
    /// <summary>
    /// Double Layer DVD-R
    /// </summary>
    DlDVDminusR =9,
    /// <summary>
    /// Double Layer DVD+RW
    /// </summary>
    DlDVDplusRW = 10,
    /// <summary>
    /// Double Layer DVD-RW
    /// </summary>
    DlDVDminusRW = 11,
    /// <summary>
    /// DVD RAM
    /// </summary>
    DVDRam = 12,
    /// <summary>
    /// Double Layer DVD RAM
    /// </summary>
    DlDVDRam = 13,

    // ToDo: Add Blueray and HDDVD...
  }
}
