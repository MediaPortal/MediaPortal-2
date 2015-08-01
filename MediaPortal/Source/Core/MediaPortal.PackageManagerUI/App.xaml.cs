#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Threading.Tasks;
using System.Windows;
using MediaPortal.Common.Logging;
using MediaPortal.PackageManager;

namespace MediaPortal.PackageManagerUI
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    #region Overrides of Application

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      _args = e.Args;

      _mainWindow = new MainWindow();

      _mainWindow.ContentRendered += MainWindowOnContentRendered;
      _mainWindow.Show();
    }

    #endregion

    private MainWindow _mainWindow;
    private string[] _args;

    private void MainWindowOnContentRendered(object sender, EventArgs e)
    {
      RunPackageManager();
    }

    private async void RunPackageManager()
    {
      var task = new Task(() =>
      {
        try
        {
          Program.Run(_args, _mainWindow);
        }
        catch (Exception ex)
        {
          ((ILogger)_mainWindow).Error("Run elevated", ex);
          Environment.ExitCode = 3;
        }
      });
      task.Start();
      await task;

      //_mainWindow.Close();
    }
  }
}
