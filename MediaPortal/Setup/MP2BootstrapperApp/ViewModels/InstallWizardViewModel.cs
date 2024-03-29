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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.ViewModels.ViewModelBuilders;
using MP2BootstrapperApp.WizardSteps;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows.Input;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallWizardViewModel : BindableBase
  {
    private readonly IBootstrapperApplicationModel _bootstrapperApplicationModel;
    private IPageViewModel _currentPage;
    private int _applyPhaseCount = 1;
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

      WireUpEventHandlers();

      InstallInitializingStep initialStep = new InstallInitializingStep(model);
      _wizard = new Wizard(initialStep);
      _wizardViewModelBuilder = new WizardStepViewModelBuilder();

      NextCommand = new DelegateCommand(() => GoNextStep(), () => _wizard.CanGoNext());
      BackCommand = new DelegateCommand(() => GoBackStep(), () => _wizard.CanGoBack());
      CancelCommand = new DelegateCommand(() => CancelInstall(), () => !_bootstrapperApplicationModel.Cancelled);
      CurrentPage = new InstallInitializingPageViewModel(initialStep);
    }

    public ICommand CancelCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }

    public IPageViewModel CurrentPage
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
          _currentPage.Detach();
          _currentPage.ButtonStateChanged -= PageButtonStateChanged;
        }
         
        _currentPage = value;

        if (_currentPage != null)
        {
          _currentPage.Attach();
          _currentPage.ButtonStateChanged += PageButtonStateChanged;
        }

        RaisePropertyChanged();
        Refresh();
      }
    }

    private void PageButtonStateChanged(object sender, EventArgs e)
    {
      Refresh();
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
      if (_wizard.Step is IFinalStep)
      {
        _dispatcher.InvokeShutdown();
      }
      else if (_wizard.GoNext())
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

    private void GoToFinishStep()
    {
      LaunchAction action = _bootstrapperApplicationModel.ActionPlan.PlannedAction;
      IStep finishStep;
      if (action == LaunchAction.Uninstall)
        finishStep = new UninstallFinishStep();
      else if (action == LaunchAction.Modify)
        finishStep = new ModifyFinishStep();
      else if (action == LaunchAction.Repair)
        finishStep = new RepairFinishStep();
      else // if (action == LaunchAction.Install)
        finishStep = new InstallFinishStep();

      GoToStep(finishStep);
    }

    private void CancelInstall()
    {
      _bootstrapperApplicationModel.Cancelled = true;
      // If currently applying the cancelled state will be returned to the engine
      // in one of its event handlers so it can cancel/roll back gracefully, else
      // it's safe to just quit now.
      if (_bootstrapperApplicationModel.InstallState != InstallState.Applying)
      {
        _dispatcher.InvokeShutdown();
      }
      else
      {
        // Refresh the can execute state of the cancel button
        Refresh();
      }
    }

    /// <summary>
    /// Fired after detection of any currently installed bundles/packages have been detected.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DetectComplete(object sender, DetectCompleteEventArgs e)
    {
      LaunchAction launchAction = _bootstrapperApplicationModel.BootstrapperApplication.LaunchAction;
      Display display = _bootstrapperApplicationModel.BootstrapperApplication.Display;

      // If the setup was launched with the uninstall action, e.g. from ARP, a later bundle being installed
      // or with a command line argument, automatically start the uninstallation if not waiting for user interaction
      // or show the uninstall confirm screen if user interaction is available.
      if (launchAction == LaunchAction.Uninstall)
      {
        if (display != Display.Full)
        {
          _bootstrapperApplicationModel.PlanAction(new SimplePlan(launchAction));
          GoToStep(new InstallationInProgressStep(_bootstrapperApplicationModel));
        }
        else
        {
          GoToStep(new UninstallStep(_bootstrapperApplicationModel));
        }
        return;
      }

      // Failure on detect, shouldn't happen unless the wix projects aren't configured
      // correctly or something has gone terribly wrong, show the error page
      if (HandleErrorResult(e.Status))
      {
        return;
      }

      // Downgrades are not supported so either close the setup if not waiting for user interaction
      // or show the downgrade page informing the user. The exception to this is the case where this
      // setup is being uninstalled by a newer version, in which case we will detect the newer version
      // but need to allow the uninstall to continue.
      if (_bootstrapperApplicationModel.IsDowngrade && launchAction != LaunchAction.Uninstall)
      {
        if (display != Display.Full)
        {
          _bootstrapperApplicationModel.LogMessage(LogLevel.Error, "Newer version of bundle detected, setup cannot continue");
          _dispatcher.InvokeShutdown();
        }
        else
        {
          GoToStep(new DowngradeStep());
        }
        return;
      }

      // If not waiting for user interaction, e.g. in hidden, passive or embedded mode, start the launched action automatically.
      // Notably this will be the case when being uninstalled by a later bundle that's currently upgrading.
      if (display != Display.Full)
      {
        _bootstrapperApplicationModel.PlanAction(new SimplePlan(launchAction));
        GoToStep(new InstallationInProgressStep(_bootstrapperApplicationModel));
        return;
      }

      // Current version installed, show the repair/modify/uninstall step
      if (_bootstrapperApplicationModel.DetectionState == DetectionState.Present && _bootstrapperApplicationModel.MainPackage.CurrentInstallState == PackageState.Present)
      {
        GoToStep(new InstallExistInstallStep(_bootstrapperApplicationModel));
      }
      // Fresh install or update, show the welcome step
      else //if (detectionState == DetectionState.Absent || detectionState == DetectionState.Newer)
      {
        GoToStep(new InstallWelcomeStep(_bootstrapperApplicationModel));
      }
    }

    /// <summary>
    /// Called when the plan phase is complete, the main <see cref="IBootstrapperApplicationModel"/>
    /// handles calling apply, this model just needs to show the error page if there was an error.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (HandleErrorResult(e.Status))
      {
        return;
      }
      // Apply automatically called in _bootstrapperApplicationModel
    }

    /// <summary>
    /// Called when the apply phase is complete.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
      Display display = _bootstrapperApplicationModel.BootstrapperApplication.Display;
      
      // If not waiting for user interaction just close regardless of success
      if (display != Display.Full)
      {
        _dispatcher.InvokeShutdown();
        return;
      }

      // Show finished, error or cancelled page
      if (Hresult.Succeeded(e.Status))
      {
        // Set progress to complete on success
        Progress = 100;
        GoToFinishStep();
      }
      else
      {
        // Set progress back to 0 on failure
        Progress = 0;

        // If the error was caused by the user cancelling the install
        // show the cancelled page rather than the error page.
        if (_bootstrapperApplicationModel.Cancelled)
        {
          GoToStep(new InstallCancelledStep());
        }
        else
        {
          GoToStep(new InstallErrorStep());
        }
      }
    }

    private void ApplyBegin(object sender, ApplyBeginEventArgs e)
    {
      // For an install this will ususally be 2, cache and execute, but for an
      // uninstall it will only be 1 as there's no cache phase.
      _applyPhaseCount = e.PhaseCount;
    }

    private void CacheAcquireProgress(object sender, CacheAcquireProgressEventArgs e)
    {
      _cacheProgress = e.OverallPercentage;
      // Total progress given the specified phase count
      Progress = (_cacheProgress + _executeProgress) / _applyPhaseCount;
      e.Cancel = _bootstrapperApplicationModel.Cancelled;
    }

    private void ExecuteProgress(object sender, ExecuteProgressEventArgs e)
    {
      _executeProgress = e.OverallPercentage;
      // Total progress given the specified phase count
      Progress = (_cacheProgress + _executeProgress) / _applyPhaseCount;
      e.Cancel = _bootstrapperApplicationModel.Cancelled;
    }

    private void Error(object sender, ErrorEventArgs e)
    {
      // This is called by the engine when there was an error applying a package, it contains
      // the info required to show a message box to allow the user to determine how to proceed.
      // TODO: Implement this.
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

    private bool HandleErrorResult(int result)
    {
      if (Hresult.Succeeded(result))
        return false;

      // If not waiting for user input, close on failure
      if (_bootstrapperApplicationModel.BootstrapperApplication.Display != Display.Full)
      {
        _dispatcher.InvokeShutdown();
      }
      // Else show error page
      else
      {
        GoToStep(new InstallErrorStep());
      }
      return true;
    }

    private void WireUpEventHandlers()
    {
      _bootstrapperApplicationModel.BootstrapperApplication.DetectComplete += DetectComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.ApplyBegin += ApplyBegin;
      _bootstrapperApplicationModel.BootstrapperApplication.ApplyComplete += ApplyComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.PlanComplete += PlanComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.CacheAcquireProgress += CacheAcquireProgress;
      _bootstrapperApplicationModel.BootstrapperApplication.ExecuteProgress += ExecuteProgress;
      _bootstrapperApplicationModel.BootstrapperApplication.Error += Error;
    }
  }
}
