#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.BundlePackages;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.Models
{
  public interface IBootstrapperApplicationModel
  {
    /// <summary>
    /// The main installation engine.
    /// </summary>
    IBootstrapperApp BootstrapperApplication { get; }

    /// <summary>
    /// Gets the handle of the application window.
    /// </summary>
    IntPtr WindowHandle { get; }

    /// <summary>
    /// The packages included in the bundle.
    /// </summary>
    ReadOnlyCollection<IBundlePackage> BundlePackages { get; }

    /// <summary>
    /// The detected state of this bundle.
    /// <see cref="DetectionState.Absent"/> if no related bundles are installed,
    /// <see cref="DetectionState.Newer"/> if this is a newer bundle than that installed,
    /// <see cref="DetectionState.Older"/> if this is an older bundle than that installed,
    /// <see cref="DetectionState.Present"/> if this same bundle is already installed.
    /// </summary>
    DetectionState DetectionState { get; }

    /// <summary>
    /// The main MP2 package.
    /// </summary>
    IBundleMsiPackage MainPackage { get; }

    /// <summary>
    /// Gets whether installing this bundle would be a downgrade of an existing installation.<br/>
    /// This is the case if this bundle or main package has a lower version than the installed version. 
    /// </summary>
    bool IsDowngrade { get; }

    /// <summary>
    /// The action that has been planned.
    /// </summary>
    IPlan ActionPlan { get; }

    /// <summary>
    /// The current state of the bootstrapper application.
    /// </summary>
    InstallState InstallState { get; }

    /// <summary>
    /// Whether the user has requested the cancellation of the installation.
    /// </summary>
    bool Cancelled { get; set; }

    /// <summary>
    /// The final result of the apply phase, returned to the engine on close.
    /// </summary>
    int FinalResult { get; }

    /// <summary>
    /// Sets the window handle of the main window, passed to the engine.
    /// </summary>
    /// <param name="view"></param>
    void SetWindowHandle(Window view);

    /// <summary>
    /// Plans the specified action and applies it if the plan was successful.
    /// </summary>
    /// <param name="actionPlan"></param>
    void PlanAction(IPlan actionPlan);

    /// <summary>
    /// Logs the specified message to the setup's log file.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    void LogMessage(LogLevel logLevel, string message);
  }
}
