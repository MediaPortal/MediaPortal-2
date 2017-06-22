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

using System.Windows.Threading;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels;
using MP2BootstrapperApp.Views;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp
{
  /// <summary>
  /// A custom bootstrapper application. 
  /// </summary>
  public class MP2BootstrapperApplication : BootstrapperApplication
  {
    public static Dispatcher Dispatcher { get; set; }

    protected override void Run()
    {
      Dispatcher = Dispatcher.CurrentDispatcher;

      var model = new BootstrapperApplicationModel(this);
      var viewModel = new StartViewModel(model);
      var view = new StartView(viewModel);

      model.SetWindowHandle(view);

      Engine.Detect();

      view.Show();
      Dispatcher.Run();
      Engine.Quit(model.FinalResult);
    }
  }
}
