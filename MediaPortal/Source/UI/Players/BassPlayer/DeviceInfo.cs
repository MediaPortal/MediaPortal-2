#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.UI.Players.BassPlayer
{
  /// <summary>
  /// Contains information about an outputdevice.
  /// </summary>
  internal struct DeviceInfo
  {
    #region Fields

    public string Name;
    public string Driver;
    public int Channels;
    public int MinRate;
    public int MaxRate;
    public TimeSpan Latency;

    #endregion

    #region Public members

    public override string ToString()
    {
      return
          String.Format(
              "Name=\"{0}\", Driver=\"{1}\", Channels={2}, MinRate={3}, MaxRate={4}, Latency={5}ms",
              Name,
              Driver,
              Channels,
              MinRate,
              MaxRate,
              Latency.TotalMilliseconds);
    }

    #endregion
  }
}