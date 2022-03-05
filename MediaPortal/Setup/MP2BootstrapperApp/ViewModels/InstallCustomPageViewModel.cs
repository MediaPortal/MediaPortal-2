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

using Microsoft.WindowsAPICodePack.Dialogs;
using MP2BootstrapperApp.WizardSteps;
using Prism.Commands;
using System.Windows.Input;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallCustomPageViewModel : AbstractPackageSelectionViewModel
  {
    public InstallCustomPageViewModel(InstallCustomStep step)
      : base(step)
    {
      Header = "Custom installation";
      SubHeader = "Un-/select the packages to install";

      BrowseCommand = new DelegateCommand(BrowsePath);
    }

    public string InstallDirectory
    {
      get { return ((InstallCustomStep)_step).InstallDirectory; }
      set 
      { 
        ((InstallCustomStep)_step).InstallDirectory = value;
        RaisePropertyChanged();
      }
    }

    public ICommand BrowseCommand { get; }

    protected void BrowsePath()
    {
      using (CommonOpenFileDialog fileDialog = new CommonOpenFileDialog())
      {
        fileDialog.IsFolderPicker = true;
        fileDialog.InitialDirectory = InstallDirectory;
        if (fileDialog.ShowDialog(((InstallCustomStep)_step).BootstrapperApplicationModel.WindowHandle) == CommonFileDialogResult.Ok)
          InstallDirectory = fileDialog.FileName;
      }
    }
  }
}
