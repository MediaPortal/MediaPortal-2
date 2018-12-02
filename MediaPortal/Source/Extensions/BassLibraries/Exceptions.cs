#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using Un4seen.Bass;

namespace MediaPortal.Extensions.BassLibraries
{
  /// <summary>
  /// Bass Player exception.
  /// </summary>
  public class BassPlayerException : Exception
  {
    public BassPlayerException(string message) : base(message) { }

    public BassPlayerException(string message, Exception innerException) : base(message, innerException) { }
  }

  /// <summary>
  /// Exception due to a failing Bass library function.
  /// </summary>
  public class BassLibraryException : BassPlayerException
  {
    #region Public members

    public BassLibraryException(string bassFunction) : this(bassFunction, Bass.BASS_ErrorGetCode()) { }

    public BassLibraryException(string bassFunction, BASSError errorCode) :
        base(string.Format("Error calling function {0}(): {1}", bassFunction, Enum.GetName(typeof(BASSError), errorCode))) { }

    #endregion
  }
}
