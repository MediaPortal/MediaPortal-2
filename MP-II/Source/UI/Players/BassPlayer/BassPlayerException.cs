#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using Un4seen.Bass;

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Bass Player exception.
  /// </summary>
  public class BassPlayerException : Exception
  {
    #region Public members

    public BassPlayerException(string message)
      : base(GetMessage(message)) { }

    public BassPlayerException(string message, Exception innerException)
      : base(GetMessage(message), innerException) { }

    #endregion

    #region Private members

    private static string GetMessage(string message)
    {
      return String.Format("BassPlayer exception: {0}", message);
    }

    #endregion

  }
}
