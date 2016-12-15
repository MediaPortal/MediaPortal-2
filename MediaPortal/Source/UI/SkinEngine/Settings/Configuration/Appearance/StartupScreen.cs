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
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;
using MediaPortal.UI.Settings;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Configuration.ConfigurationClasses;

namespace MediaPortal.UI.SkinEngine.Settings.Configuration.Appearance
{
  public class StartupScreen : SingleSelectionList
  {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal class DisplayDevice
    {
      public int cb = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
      public string DeviceName = new String(' ', 32);
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceString = new String(' ', 128);
      public int StateFlags = 0;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceID = new String(' ', 128);
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string DeviceKey = new String(' ', 128);
    }

    [DllImport("user32.dll")]
    internal static extern bool EnumDisplayDevices(string lpDevice, int iDevNum, [In, Out] DisplayDevice lpDisplayDevice, int dwFlags);

    #region Protected fields

    protected IList<Screen> _screens;

    #endregion

    #region Base overrides

    public override void Load()
    {
      _screens = new List<Screen>(Screen.AllScreens);

      IList<String> options = new List<String>();
      options.Add("[Settings.Appearance.System.StartupScreen.DoNotUseSelection]");

      foreach (Screen screen in Screen.AllScreens)
      {
        const int dwf = 0;
        DisplayDevice info = new DisplayDevice();
        string monitorname = null;
        info.cb = Marshal.SizeOf(info);
        if (EnumDisplayDevices(screen.DeviceName, 0, info, dwf))
          monitorname = info.DeviceString;
        options.Add(string.Format("{0} ({1}x{2})", monitorname, screen.Bounds.Width, screen.Bounds.Height));
      }

      int startupScreenNum = SettingsManager.Load<StartupSettings>().StartupScreenNum;

      if (startupScreenNum < Screen.AllScreens.Length)
        Selected = SettingsManager.Load<StartupSettings>().StartupScreenNum + 1;
      else
        Selected = 0;

      _items = options.Select(LocalizationHelper.CreateResourceString).ToList();
    }

    public override void Save()
    {
      StartupSettings settings = SettingsManager.Load<StartupSettings>();
      settings.StartupScreenNum = Selected - 1;
      SettingsManager.Save(settings);
    }

    #endregion
  }
}
