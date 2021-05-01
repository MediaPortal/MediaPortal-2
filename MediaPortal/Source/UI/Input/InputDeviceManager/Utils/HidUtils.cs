#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.InputManagement;
using System;

namespace MediaPortal.Plugins.InputDeviceManager.Utils
{
  public static class HidUtils
  {
    /// <summary>
    /// Provide the type for the usage corresponding to the given usage page.
    /// </summary>
    /// <param name="aUsagePage">The usage page to get the usage for.</param>
    /// <returns>The corresponding usage type, or null if no usage type is known.</returns>
    public static Type UsageType(UsagePage aUsagePage)
    {
      switch (aUsagePage)
      {
        case UsagePage.GenericDesktopControls:
          return typeof(GenericDesktop);

        case UsagePage.Consumer:
          return typeof(ConsumerControl);

        case UsagePage.WindowsMediaCenterRemoteControl:
          return typeof(WindowsMediaCenterRemoteControl);

        case UsagePage.Telephony:
          return typeof(TelephonyDevice);

        case UsagePage.SimulationControls:
          return typeof(SimulationControl);

        case UsagePage.GameControls:
          return typeof(GameControl);

        default:
          return null;
      }
    }
  }
}
