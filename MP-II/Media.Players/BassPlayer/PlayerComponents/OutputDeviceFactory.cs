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

using System;

namespace Media.Players.BassPlayer
{
  public partial class BassPlayer
  {
    partial class OutputDeviceManager
    {
      /// <summary>
      /// 
      /// </summary>
      partial class OutputDeviceFactory : IDisposable
      {
        #region Fields

        BassPlayer _Player;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        #region Public members

        public OutputDeviceFactory(BassPlayer player)
        {
          _Player = player;
        }

        /// <summary>
        /// Creates an IOutputDevice object based on usersettings.
        /// </summary>
        /// <returns></returns>
        public IOutputDevice CreateOutputDevice()
        {
          IOutputDevice outputDevice;
          switch (_Player.Settings.OutputMode)
          {
            case OutputMode.DirectSound:
              outputDevice = DirectXOutputDevice.Create(_Player);
              break;

            case OutputMode.ASIO:
              outputDevice = ASIOOutputDevice.Create(_Player);
              break;

            default:
              throw new BassPlayerException(String.Format("Unknown constant OutputMode.{0}", _Player.Settings.OutputMode));
          }
          return outputDevice;
        }

        #endregion
      }
    }
  }
}