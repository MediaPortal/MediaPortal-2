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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.RefreshRateChanger
{
  /// <summary>
  /// Workaround for an issue with stuttering in Windows 7 after refresh rate changes, caused by DWM.
  /// </summary>
  internal class Windows7DwmFix
  {
    [DllImport("dwmapi.dll")]
    private static extern int DwmIsCompositionEnabled(ref int pfEnabled);

    // Fix for Mantis 0002608
    public class SuicideForm : Form
    {
      public SuicideForm()
      {
        Thread.Sleep(300);
        Activated += SuicideFormActivated;
        Opacity = 0;
      }

      protected override void Dispose(bool disposing)
      {
        Activated -= SuicideFormActivated;
        base.Dispose(disposing);
      }

      private void SuicideFormActivated(Object sender, EventArgs e)
      {
        Thread.Sleep(500);
        Close();
      }
    }

    public static void KillFormThread()
    {
      try
      {
        var suicideForm = new SuicideForm();
        suicideForm.Show();
        suicideForm.Focus();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CycleRefresh: KillFormThread exception", ex);
      }
    }

    protected static bool IsWin8OrLater()
    {
      var ver = Environment.OSVersion;
      return ver.Version.Major >= 6 && ver.Version.Minor >= 2;
    }

    public static void FixDwm()
    {
      if (IsWin8OrLater())
        return;

      try
      {
        int dwmEnabled = 0;
        DwmIsCompositionEnabled(ref dwmEnabled);

        if (dwmEnabled > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug("CycleRefresh: DWM Detected, performing shenanigans");
          ThreadStart starter = KillFormThread;
          var killFormThread = new Thread(starter) { IsBackground = true };
          killFormThread.Start();
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CycleRefresh: FixDwm exception", ex);
      }
    }
  }
}
