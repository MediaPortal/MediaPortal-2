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
using MediaPortal.Extensions.BassLibraries;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Players.BassPlayer.OutputDevices;
using MediaPortal.UI.Players.BassPlayer.Settings;

namespace MediaPortal.UI.Players.BassPlayer.PlayerComponents
{
  public class OutputDeviceFactory : IDisposable
  {
    #region Protected fields

    protected Controller _controller;

    #endregion

    public OutputDeviceFactory(Controller controller)
    {
      _controller = controller;
    }

    #region IDisposable Members

    public void Dispose()
    {
    }

    #endregion

    #region Public members

    /// <summary>
    /// Creates an IOutputDevice object based on usersettings.
    /// </summary>
    /// <returns></returns>
    public IOutputDevice CreateOutputDevice()
    {
      IOutputDevice outputDevice;
      BassPlayerSettings settings = Controller.GetSettings();

      switch (settings.OutputMode)
      {
        case OutputMode.DirectSound:
          outputDevice = new DirectXOutputDevice(_controller);
          break;
        case OutputMode.WASAPI:
          outputDevice = new WASAPIOutputDevice(_controller);
          break;
        default:
          throw new BassPlayerException(String.Format("Unimplemented audio output mode {0}", settings.OutputMode));
      }
      return outputDevice;
    }

    #endregion
  }
}
