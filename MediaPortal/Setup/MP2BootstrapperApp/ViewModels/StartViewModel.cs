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
using System.Windows.Input;
using MP2BootstrapperApp.Models;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Commands;
using Prism.Mvvm;

namespace MP2BootstrapperApp.ViewModels
{
  /// <summary>
  /// View model for the <see cref="MP2BootstrapperApplication"/>
  /// </summary>
  public class StartViewModel : BindableBase
  {
    public enum InstallState
    {
      Initializing,
      Present,
      NotPresent,
      Applaying,
      Canceled
    }

    #region Fields

    private readonly BootstrapperApplicationModel _model;
    private InstallState _state;
    private string _message;
    private bool _installServer;
    private bool _installClient;

    #endregion

    #region Constructors and destructor

    public StartViewModel(BootstrapperApplicationModel model)
    {
      _model = model;
      State = InstallState.Initializing;

      WireUpEventHandlers();

      ClientServerInstallCommand = new DelegateCommand(() => ClientServerInstall(), () => true);
      ClientInstallCommand = new DelegateCommand(() => ClientInstall(), () => true);
      ServerInstallCommand = new DelegateCommand(() => ServerInstall(), () => true);
      CancelCommand = new DelegateCommand(() => CancelInstall(), () => State != InstallState.Canceled);
      UninstallCommand = new DelegateCommand(() => _model.PlanAction(LaunchAction.Uninstall), () => State == InstallState.Present);
    }

    private void ClientServerInstall()
    {
      _installClient = true;
      _installServer = true;
      _model.PlanAction(LaunchAction.Install);
    }

    private void ClientInstall()
    {
      _installClient = true;
      _model.PlanAction(LaunchAction.Install);
    }

    private void ServerInstall()
    {
      _installServer = true;
      _model.PlanAction(LaunchAction.Install);
    }

    private void CancelInstall()
    {
      _model.LogMessage("Cancelling...");
      if (State == InstallState.Applaying)
      {
        State = InstallState.Canceled;
      }
      else
      {
        MP2BootstrapperApplication.Dispatcher.InvokeShutdown();
      }
    }

    #endregion

    #region Properties

    public ICommand UninstallCommand { get; private set; }

    public ICommand CancelCommand { get; private set; }

    public ICommand ClientServerInstallCommand { get; private set; }

    public ICommand ClientInstallCommand { get; private set; }

    public ICommand ServerInstallCommand { get; private set; }

    public ICommand CustomInstallCommand { get; private set; }

    public string Message
    {
      get { return _message; }
      set
      {
        if (_message != value)
        {
          SetProperty(ref _message, value);
        }
      }
    }

    public InstallState State
    {
      get { return _state; }
      set
      {
        if (_state != value)
        {
          Message = "Status: " + _state;
          SetProperty(ref _state, value);
          Refresh();
        }
      }
    }

    #endregion

    #region Methods

    protected void DetectedPackageComplete(object sender, DetectPackageCompleteEventArgs e)
    {
      if (e.PackageId.Equals("MP2-Setup.msi", StringComparison.Ordinal))
      {
        State = e.State == PackageState.Present ? InstallState.Present : InstallState.NotPresent;
      }
    }

    protected void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (State == InstallState.Canceled)
      {
        MP2BootstrapperApplication.Dispatcher.InvokeShutdown();
        return;
      }

      _model.ApplyAction();
    }

    protected void ApplyBegin(object sender, ApplyBeginEventArgs e)
    {
      State = InstallState.Applaying;
    }

    protected void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
    {
      if (State == InstallState.Canceled)
      {
        e.Result = Result.Cancel;
      }
    }

    protected void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
    {
      if (State == InstallState.Canceled)
      {
        e.Result = Result.Cancel;
      }
    }

    protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
      _model.FinalResult = e.Status;
      MP2BootstrapperApplication.Dispatcher.InvokeShutdown();
    }

    protected void PlanMsiFeature(object sender, PlanMsiFeatureEventArgs e)
    {
      if (e.FeatureId == "Client")
      {
        e.State = _installClient ? FeatureState.Local : FeatureState.Absent;
      }
      else
      {
        e.State = FeatureState.Local;
      }

      if (e.FeatureId == "Server")
      {
        e.State = _installServer ? FeatureState.Local : FeatureState.Absent;
      }
      else
      {
        e.State = FeatureState.Local;
      }
    }

    protected void PlanPackageBegin(object sender, PlanPackageBeginEventArgs e)
    {
      if (e.PackageId == "directx9")
      {
        e.State = _installServer ? RequestState.None : RequestState.Present;
      }
    }

    private void Refresh()
    {
      MP2BootstrapperApplication.Dispatcher.Invoke(() =>
      {
        ((DelegateCommand)ClientServerInstallCommand).RaiseCanExecuteChanged();
        ((DelegateCommand)ClientInstallCommand).RaiseCanExecuteChanged();
        ((DelegateCommand)ServerInstallCommand).RaiseCanExecuteChanged();
        ((DelegateCommand)UninstallCommand).RaiseCanExecuteChanged();
        ((DelegateCommand)CancelCommand).RaiseCanExecuteChanged();
      });
    }

    private void WireUpEventHandlers()
    {
      _model.BootstrapperApplication.DetectPackageComplete += DetectedPackageComplete;
      _model.BootstrapperApplication.PlanComplete += PlanComplete;
      _model.BootstrapperApplication.ApplyComplete += ApplyComplete;
      _model.BootstrapperApplication.ApplyBegin += ApplyBegin;
      _model.BootstrapperApplication.ExecutePackageBegin += ExecutePackageBegin;
      _model.BootstrapperApplication.ExecutePackageComplete += ExecutePackageComplete;
      _model.BootstrapperApplication.PlanMsiFeature += PlanMsiFeature;
      _model.BootstrapperApplication.PlanPackageBegin += PlanPackageBegin;
    }

    #endregion
  }
}
