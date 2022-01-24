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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using Prism.Commands;
using Prism.Mvvm;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallWizardViewModel : BindableBase
  {
    public enum InstallState
    {
      Initializing,
      Present,
      NotPresent,
      Applaying,
      Canceled
    }

    private readonly IBootstrapperApplicationModel _bootstrapperApplicationModel;
    private InstallWizardPageViewModelBase _currentPage;
    private string _header;
    private string _buttonNextContent;
    private string _buttonBackContent;
    private string _buttonCancelContent;
    private InstallState _state;
    private int _progress;
    private int _cacheProgress;
    private int _executeProgress;
    private readonly Wizard _wizard;
    private readonly IDispatcher _dispatcher;
    private WizardStepViewModelBuilder _wizardViewModelBuilder;

    public InstallWizardViewModel(IBootstrapperApplicationModel model, IDispatcher dispatcher)
    {
      _bootstrapperApplicationModel = model;
      _dispatcher = dispatcher;
      State = InstallState.Initializing;

      WireUpEventHandlers();

      InstallWelcomeStep welcomeStep = new InstallWelcomeStep(model);
      _wizard = new Wizard(welcomeStep);
      _wizardViewModelBuilder = new WizardStepViewModelBuilder();

      NextCommand = new DelegateCommand(() => GoNextStep(), () => _wizard.CanGoNext());
      BackCommand = new DelegateCommand(() => GoBackStep(), () => _wizard.CanGoBack());
      CancelCommand = new DelegateCommand(() => CancelInstall(), () => State != InstallState.Canceled);
      CurrentPage = new InstallWelcomePageViewModel(welcomeStep);
    }

    public InstallState State
    {
      get { return _state; }
      set
      {
        if (_state != value)
        {
          SetProperty(ref _state, value);
          Refresh();
        }
      }
    }

    public string Header
    {
      get { return _header; }
      set { SetProperty(ref _header, value); }
    }

    public string ButtonNextContent
    {
      get { return _buttonNextContent; }
      set { SetProperty(ref _buttonNextContent, value); }
    }

    public string ButtonBackContent
    {
      get { return _buttonBackContent; }
      set { SetProperty(ref _buttonBackContent, value); }
    }

    public string ButtonCancelContent
    {
      get { return _buttonCancelContent; }
      set { SetProperty(ref _buttonCancelContent, value); }
    }

    public ICommand CancelCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }

    public InstallWizardPageViewModelBase CurrentPage
    {
      get { return _currentPage; }
      set
      {
        if (value == _currentPage)
        {
          return;
        }

        if (_currentPage != null)
        {
          _currentPage.IsCurrentPage = false;
        }
         
        _currentPage = value;

        if (_currentPage != null)
        {
          _currentPage.IsCurrentPage = true;
          _currentPage.WizardViewModel = this;
        }

        RaisePropertyChanged();
        Refresh();
      }
    }

    public int Progress
    {
      get { return _progress; }
      set
      {
        _progress = value;
        RaisePropertyChanged();
      }
    }

    private void GoNextStep()
    {
      if (_wizard.GoNext())
      {
        CurrentPage = _wizardViewModelBuilder.GetViewModel(_wizard.Step);
      }
    }

    private void GoToStep(IStep step)
    {
      if (_wizard.Push(step))
      {
        CurrentPage = _wizardViewModelBuilder.GetViewModel(_wizard.Step);
      }
    }

    private void GoBackStep()
    {
      if (_wizard.GoBack())
      {
        CurrentPage = _wizardViewModelBuilder.GetViewModel(_wizard.Step);
      }
    }

    private void CancelInstall()
    {
      if (State == InstallState.Applaying)
      {
        State = InstallState.Canceled;
      }
      else
      {
        _dispatcher.InvokeShutdown();
      }
    }

    private void DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
      GoToStep(new InstallExistInstallStep(_bootstrapperApplicationModel));
    }

    protected void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (State == InstallState.Canceled)
      {
        _dispatcher.InvokeShutdown();
        return;
      }
      _bootstrapperApplicationModel.ApplyAction();
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
      GoToStep(new InstallFinishStep(_dispatcher));
    }

    private void CacheAcquireProgress(object sender, CacheAcquireProgressEventArgs e)
    {
      _cacheProgress = e.OverallPercentage;
      Progress = (_cacheProgress + _executeProgress) / 2;
    }

    private void ExecuteProgress(object sender, ExecuteProgressEventArgs e)
    {
      _executeProgress = e.OverallPercentage;
      Progress = (_cacheProgress + _executeProgress) / 2;
    }

    private void Refresh()
    {
      _dispatcher.Invoke(() =>
      {
        ((DelegateCommand) NextCommand).RaiseCanExecuteChanged();
        ((DelegateCommand) BackCommand).RaiseCanExecuteChanged();
        ((DelegateCommand) CancelCommand).RaiseCanExecuteChanged();
      });
    }

    private void WireUpEventHandlers()
    {
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperDetectRelatedBundle += DetectRelatedBundle;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperPlanComplete += PlanComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperApplyComplete += ApplyComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperApplyBegin += ApplyBegin;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperExecutePackageBegin += ExecutePackageBegin;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperExecutePackageComplete += ExecutePackageComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperCacheAcquireProgress += CacheAcquireProgress;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperExecuteProgress += ExecuteProgress;
    }
  }
}
