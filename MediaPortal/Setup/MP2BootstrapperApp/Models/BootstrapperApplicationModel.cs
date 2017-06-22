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
using System.Windows;
using System.Windows.Interop;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// Model class for the <see cref="MP2BootstrapperApplication"/>
  /// </summary>
  public class BootstrapperApplicationModel
  {
    #region Fields

    private IntPtr _hwnd;

    #endregion

    #region Constructors and destructor

    public BootstrapperApplicationModel(BootstrapperApplication bootstreApplication)
    {
      BootstrapperApplication = bootstreApplication;
      _hwnd = IntPtr.Zero;
    }

    #endregion

    #region Properties

    public BootstrapperApplication BootstrapperApplication { get; set; }

    public int FinalResult { get; set; }

    #endregion

    #region Public methods

    public void SetWindowHandle(Window view)
    {
      _hwnd = new WindowInteropHelper(view).Handle;
    }

    public void PlanAction(LaunchAction action)
    {
      BootstrapperApplication.Engine.Plan(action);
    }

    public void ApplyAction()
    {
      BootstrapperApplication.Engine.Apply(_hwnd);
    }

    public void LogMessage(string message)
    {
      BootstrapperApplication.Engine.Log(LogLevel.Verbose, message);
    }

    #endregion

  }
}
