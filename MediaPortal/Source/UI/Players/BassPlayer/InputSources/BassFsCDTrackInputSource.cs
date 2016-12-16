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

namespace MediaPortal.UI.Players.BassPlayer.InputSources
{
  /// <summary>
  /// Static wrapper class to create a CD track inputsource by parsing a virtual filesystem CD track file path.
  /// </summary>
  internal static class BassFsCDTrackInputSource
  {
    /// <summary>
    /// Creates and initializes an instance of <see cref="BassCDTrackInputSource"/> using a given virtual audio CD file path.
    /// </summary>
    /// <param name="cdTrackFilePath">The file path of the CD track in the form <c>"D:\\Track03.cda"</c> to be handled by the instance.</param>
    /// <returns>The new instance of <see cref="BassCDTrackInputSource"/>.</returns>
    public static BassCDTrackInputSource Create(string cdTrackFilePath)
    {
      if (string.IsNullOrEmpty(cdTrackFilePath) || cdTrackFilePath.Length != 14)
        throw new ArgumentException(string.Format("Given file path '{0}' is no audio CD file path of the form 'D:\\Track03.cda'", cdTrackFilePath));
      char drive = char.ToUpper(cdTrackFilePath[0]);
      byte trackNo = byte.Parse(cdTrackFilePath.Substring(8, 2));
      return BassCDTrackInputSource.Create(drive, trackNo);
    }
  }
}