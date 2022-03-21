﻿#region Copyright (C) 2007-2021 Team MediaPortal

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

using System.Windows;
using System.Windows.Input;
using MP2BootstrapperApp.ViewModels;

namespace MP2BootstrapperApp.Views
{
  /// <summary>
  /// Interaction logic for InstallWizardBaseView.xaml
  /// </summary>
  public partial class InstallWizardView : Window
  {
    public InstallWizardView(InstallWizardViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;

      Closed += (sender, e) => viewModel.CancelCommand.Execute(this);
    }

    /// <summary>
    /// Workaround method to allow move the main window.
    /// Needed because WindowsStyle is set to "None"
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.LeftButton == MouseButtonState.Pressed)
      {
        DragMove();
      }

    }
  }
}
