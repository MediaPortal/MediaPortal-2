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
using Ui.Players.BassPlayer.Interfaces;
using Ui.Players.BassPlayer.OutputDevices;

namespace Ui.Players.BassPlayer.PlayerComponents
{
  public class OutputDeviceFactory : IDisposable
  {
    #region Protected fields

    protected BassPlayer _player;

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region Public members

    public OutputDeviceFactory(BassPlayer player)
    {
      _player = player;
    }

    /// <summary>
    /// Creates an IOutputDevice object based on usersettings.
    /// </summary>
    /// <returns></returns>
    public IOutputDevice CreateOutputDevice()
    {
      IOutputDevice outputDevice;
      switch (_player.Settings.OutputMode)
      {
        case OutputMode.DirectSound:
          outputDevice = DirectXOutputDevice.Create(_player);
          break;

        case OutputMode.ASIO:
          outputDevice = ASIOOutputDevice.Create(_player);
          break;

        default:
          throw new BassPlayerException(String.Format("Unknown constant AudioOutputMode.{0}", _player.Settings.OutputMode));
      }
      return outputDevice;
    }

    #endregion
  }
}
