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
using Ui.Players.BassPlayer.Settings;

namespace Ui.Players.BassPlayer.PlayerComponents
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

        default:
          throw new BassPlayerException(String.Format("Unimplemented audio output mode {0}", settings.OutputMode));
      }
      return outputDevice;
    }

    #endregion
  }
}
