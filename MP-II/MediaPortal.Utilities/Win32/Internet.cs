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

namespace MediaPortal.Utilities.Win32
{
  /// <summary>
  /// Contains Internet Related Methods
  /// </summary>
  public class Internet
  {
    /// <summary>
    /// Checks if the computer is connected to the internet...
    /// </summary>
    /// <returns>Connection State</returns>
    public static bool IsConnectedToInternet()
    {
#if DEBUG
      return true;
#else
      int Desc;
      //return InternetGetConnectedState(out Desc, 0);
      return true;
#endif
    }

    public static bool IsConnectedToInternet(ref int code)
    {
#if DEBUG
      return true;
#else
      //return InternetGetConnectedState(out code, 0);
      return true;
#endif
    }


  }
}
