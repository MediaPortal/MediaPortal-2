﻿#region Copyright (C) 2007-2017 Team MediaPortal

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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels;
using MP2BootstrapperApp.Views;
using System;
using System.Windows;

namespace MP2BootstrapperApp
{
  /// <summary>
  /// A custom bootstrapper application. 
  /// </summary>
  public class MP2BootstrapperApplication : BootstrapperApplicationWrapper
  {
    private IDispatcher _dispatcher;

    protected override void Run()
    {
      AppDomain.CurrentDomain.UnhandledException += UnhandledException;

      _dispatcher = new DispatcherWrapper();

#if DEBUG
      MessageBox.Show("dd");
#endif

      IBootstrapperApplicationModel model = new BootstrapperApplicationModel(this);
      InstallWizardViewModel viewModel = new InstallWizardViewModel(model, _dispatcher);
      InstallWizardView view = new InstallWizardView(viewModel);

      model.SetWindowHandle(view);

      Engine.Detect();

      if (Command.Display == Display.Full || Command.Display == Display.Passive)
        view.Show();

      _dispatcher.Run();
      Engine.Quit(model.FinalResult);
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception exception = e.ExceptionObject as Exception;
      Log(LogLevel.Error, $"MP2BootstrapperApplication: Unhandled exception - {exception?.Message}\r\n{exception?.StackTrace}");
    }
  }
}
