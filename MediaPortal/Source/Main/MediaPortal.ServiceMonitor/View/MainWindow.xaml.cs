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
using System.ComponentModel;
using System.Windows;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.ServiceMonitor.Settings;
using MediaPortal.ServiceMonitor.ViewModel;

namespace MediaPortal.ServiceMonitor.View
{
  /// <summary>
  /// Interaction logic for main window.
  /// </summary>
  public partial class MainWindow
  {
    #region Ctor

    public MainWindow()
    {
      InitializeComponent();

      ViewModel = ServiceRegistration.Get<IAppController>();

      LoadSettings();
    }

    #endregion

    #region Event Handler: Loaded, Closing

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      LoadSettings();
    }

    private void OnClosed(object sender, EventArgs e)
    {
      SaveSettings();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      // We don't want to close the window, just minimize to tray
      e.Cancel = true;
      WindowState = WindowState.Minimized;
    }

    #endregion

    #region Properties

    public IAppController ViewModel
    {
      get { return (IAppController) DataContext; }
      internal set { DataContext = value; }
    }

    #endregion

    #region Settings

    private void LoadSettings()
    {
      ServiceRegistration.Get<ILogger>().Debug("MainWindow:LoadSettings");
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<ServiceMonitorSettings>();

      Top = settings.Top;
      Left = settings.Left;
      Height = settings.Height;
      Width = settings.Width;

      // Very quick and dirty - but it does the job
      if (settings.Maximised)
        WindowState = WindowState.Maximized;
    }

    private void SaveSettings()
    {
      ServiceRegistration.Get<ILogger>().Debug("MainWindow:SaveSettings");
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var settings = settingsManager.Load<ServiceMonitorSettings>();

      if (WindowState == WindowState.Maximized)
      {
        // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
        settings.Top = RestoreBounds.Top;
        settings.Left = RestoreBounds.Left;
        settings.Height = RestoreBounds.Height;
        settings.Width = RestoreBounds.Width;
        settings.Maximised = true;
      }
      else
      {
        settings.Top = Top;
        settings.Left = Left;
        settings.Height = Height;
        settings.Width = Width;
        settings.Maximised = false;
      }

      settingsManager.Save(settings);
    }

    #endregion
  }
}
