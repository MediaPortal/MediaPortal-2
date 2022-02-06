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

using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Input;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallWizardViewModel : BindableBase
  {
    private readonly IBootstrapperApplicationModel _bootstrapperApplicationModel;
    private InstallWizardPageViewModelBase _currentPage;
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

      InstallWelcomeStep welcomeStep = new InstallWelcomeStep(model);
      _wizard = new Wizard(welcomeStep);
      _wizardViewModelBuilder = new WizardStepViewModelBuilder();

      NextCommand = new DelegateCommand(() => GoNextStep(), () => _wizard.CanGoNext());
      BackCommand = new DelegateCommand(() => GoBackStep(), () => _wizard.CanGoBack());
      CancelCommand = new DelegateCommand(() => CancelInstall(), () => !_bootstrapperApplicationModel.Cancelled);
      CurrentPage = new InstallWelcomePageViewModel(welcomeStep);
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
          _currentPage.Detach();
        }
         
        _currentPage = value;

        if (_currentPage != null)
        {
          _currentPage.Attach();
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
      _bootstrapperApplicationModel.Cancelled = true;
      // If currently applying the cancelled state will be returned to the engine
      // in one of it;s event handlers so if can cancel/roll back gracefully, else
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
      LaunchAction launchAction = _bootstrapperApplicationModel.BootstrapperApplication.Command.Action;
      // If the setup was launched with the uninstall action, e.g. from ARP, a later bundle being installed
      // or with a command line argument, automatically start the uninstallation.
      if (launchAction == LaunchAction.Uninstall)
      {
        _bootstrapperApplicationModel.PlanAction(launchAction);
        GoToStep(new InstallationInProgressStep(_bootstrapperApplicationModel));
        return;
      }

      // Failure on detect, shouldn't happen unless the wix projects aren't configured
      // correctly or something has gone terribly wrong, show the error page
      if (!Hresult.Succeeded(e.Status))
      {
        GoToStep(new InstallErrorStep(_dispatcher));
        return;
      }

      Display display = _bootstrapperApplicationModel.BootstrapperApplication.Command.Display;
      // If not waiting for user interaction, e.g. in hidden, passive or embedded mode, start the launched action automatically.
      // Notably this will be the case when being uninstalled by a later bundle that's currently upgrading.
      if (display != Display.Full)
      {
        _bootstrapperApplicationModel.PlanAction(launchAction);
        GoToStep(new InstallationInProgressStep(_bootstrapperApplicationModel));
        return;
      }

      DetectionState detectionState = _bootstrapperApplicationModel.DetectionState;
      // Current version installed, show the repair/modify/uninstall step
      if (detectionState == DetectionState.Present)
      {
        GoToStep(new InstallExistInstallStep(_bootstrapperApplicationModel));
      }
      // Different version installed, show upgrade step.
      // ToDo: Downgrade step?
      else if (detectionState == DetectionState.Newer || detectionState == DetectionState.Older)
      {
        GoToStep(new UpdateStep(_bootstrapperApplicationModel));
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
      if (!Hresult.Succeeded(e.Status))
      {
        GoToStep(new InstallErrorStep(_dispatcher));
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
      Display display = _bootstrapperApplicationModel.BootstrapperApplication.Command.Display;
      
      // If not waiting for user interaction just close regardless of success
      if (display != Display.Full)
      {
        _dispatcher.InvokeShutdown();
        return;
      }

      // Show finished, error or cancelled page
      if (Hresult.Succeeded(e.Status))
      {
        GoToStep(new InstallFinishStep(_dispatcher));
      }
      else
      {
        // If the error was caused by the user cancelling the install
        // show the cancelled page rather than the error page.
        if (_bootstrapperApplicationModel.Cancelled)
        {
          GoToStep(new InstallCancelledStep(_dispatcher));
        }
        else
        {
          GoToStep(new InstallErrorStep(_dispatcher));
        }
      }
    }

    /// <summary>
    /// Event which contains the number of phases in the apply phase.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ApplyPhaseCount(object sender, ApplyPhaseCountArgs e)
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
      e.Result = _bootstrapperApplicationModel.Cancelled ? Result.Cancel : Result.Ok;
    }

    private void ExecuteProgress(object sender, ExecuteProgressEventArgs e)
    {
      _executeProgress = e.OverallPercentage;
      // Total progress given the specified phase count
      Progress = (_cacheProgress + _executeProgress) / _applyPhaseCount;
      e.Result = _bootstrapperApplicationModel.Cancelled ? Result.Cancel : Result.Ok;
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

    private void WireUpEventHandlers()
    {
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperDetectComplete += DetectComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperApplyComplete += ApplyComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperPlanComplete += PlanComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperApplyPhaseCount += ApplyPhaseCount;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperCacheAcquireProgress += CacheAcquireProgress;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperExecuteProgress += ExecuteProgress;
      _bootstrapperApplicationModel.BootstrapperApplication.WrapperError += Error;
    }
  }
}
