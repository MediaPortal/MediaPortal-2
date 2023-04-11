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

using Microsoft.WindowsAPICodePack.Dialogs;
using MP2BootstrapperApp.WizardSteps;
using Prism.Commands;
using System.Windows.Input;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallCustomPropertiesPageViewModel : InstallWizardPageViewModelBase<InstallCustomPropertiesStep>
  {
    protected PropertyValidationViewModel<string> _installDirectory;

    public InstallCustomPropertiesPageViewModel(InstallCustomPropertiesStep step)
      : base(step)
    {
      _installDirectory = new PropertyValidationViewModel<string>(step.IsValidInstallDirectory);
      _installDirectory.Value = step.InstallDirectory;
      _installDirectory.PropertyChanged += InstallDirectoryChanged;

      Header = "[InstallCustomPropertiesPageView.Header]";
      SubHeader = "[InstallCustomPropertiesPageView.SubHeader]";
      BrowseCommand = new DelegateCommand(BrowsePath);
    }

    private void InstallDirectoryChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(_installDirectory.Value))
        _step.InstallDirectory = _installDirectory.Value;
    }

    public PropertyValidationViewModel<string> InstallDirectory
    {
      get { return _installDirectory; }
    }

    public bool CreateDesktopShortcuts
    {
      get { return _step.CreateDesktopShortcuts; }
      set
      {
        _step.CreateDesktopShortcuts = value;
        RaisePropertyChanged();
      }
    }

    public bool CreateStartMenuShortcuts
    {
      get { return _step.CreateStartMenuShortcuts; }
      set
      {
        _step.CreateStartMenuShortcuts = value;
        RaisePropertyChanged();
      }
    }

    public ICommand BrowseCommand { get; }

    protected void BrowsePath()
    {
      using (CommonOpenFileDialog fileDialog = new CommonOpenFileDialog())
      {
        fileDialog.IsFolderPicker = true;
        fileDialog.InitialDirectory = InstallDirectory.Value;
        if (fileDialog.ShowDialog(_step.BootstrapperApplicationModel.WindowHandle) == CommonFileDialogResult.Ok)
          InstallDirectory.Value = fileDialog.FileName;
      }
    }
  }
}
