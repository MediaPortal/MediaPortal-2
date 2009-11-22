#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    partial class OutputDeviceManager
    {
      partial class OutputDeviceFactory
      {
        /// <summary>
        /// Contains information about an outputdevice.
        /// </summary>
        struct DeviceInfo
        {
          #region Fields

          public string _Name;
          public string _Driver;
          public int _Channels;
          public int _MinRate;
          public int _MaxRate;
          public TimeSpan _Latency;

          #endregion

          #region Public members

          public override string ToString()
          {
            return
                String.Format(
                    "Name=\"{0}\", Driver=\"{1}\", Channels={2}, MinRate={3}, MaxRate={4}, Latency={5}ms",
                    _Name,
                    _Driver,
                    _Channels,
                    _MinRate,
                    _MaxRate,
                    _Latency.TotalMilliseconds);
          }

          #endregion
        }
      }
    }
  }
}