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

using System.Windows;

namespace MediaPortal.ServiceMonitor.ViewModel
{
  public interface IAppController
  {
    bool IsAutoStartEnabled { get; set; }

    /// <summary>
    /// Displays the main application window and assigns
    /// it as the application's <see cref="Application.MainWindow"/>.
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// Minimizes the application to the system tray.
    /// </summary>
    void HideMainWindow();

    /// <summary>
    /// Closes the main window and exits the application.
    /// </summary>
    void CloseMainApplication();

    /// <summary>
    /// Check if the MP2 Server Service is installed
    /// </summary>
    /// <returns></returns>
    bool IsServerServiceInstalled();

    /// <summary>
    /// Verify if the MP2 Server Service is Started
    /// </summary>
    /// <returns></returns>
    bool IsServerServiceRunning();

    /// <summary>
    /// Start the MP2 Server Service
    /// </summary>
    /// <returns></returns>
    bool StartServerService();

    /// <summary>
    /// Stop the MP2 Server Service
    /// </summary>
    /// <returns></returns>
    bool StopServerService();
  }
}