
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
using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using System;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallationInProgressPageViewModel : InstallWizardPageViewModelBase
  {
    protected IBootstrapperApplicationModel _bootstrapperModel;

    protected int _cacheProgress;
    protected int _executeProgress;
    protected int _progress;
    protected string _currentAction;

    public InstallationInProgressPageViewModel(InstallationInProgressStep step)
      : base(step)
    {
      _bootstrapperModel = step.BootstrapperApplicationModel;
      _currentAction = "Processing ...";

      Header = GetHeaderForAction(_bootstrapperModel.PlannedAction);
    }

    public int Progress
    {
      get { return _progress; }
      set { SetProperty(ref _progress, value); }
    }

    public string CurrentAction
    {
      get { return _currentAction; }
      set { SetProperty(ref _currentAction, value); }
    }

    public override void Attach()
    {
      base.Attach();
      AttachProgressHandlers();
    }

    public override void Detach()
    {
      base.Detach();
      DetachProgressHandlers();
    }

    private string GetHeaderForAction(LaunchAction action)
    {
      switch (action)
      {
        case LaunchAction.Install:
          return "Installing";
        case LaunchAction.Modify:
          return "Modifying";
        case LaunchAction.Repair:
          return "Repairing";
        case LaunchAction.Uninstall:
          return "Uninstalling";
        default:
          return "Processing";
      }
    }

    private void AttachProgressHandlers()
    {
      _bootstrapperModel.BootstrapperApplication.WrapperCacheAcquireProgress += CacheAquireProgress;
      _bootstrapperModel.BootstrapperApplication.WrapperExecuteProgress += ExecuteProgress;
    }

    private void DetachProgressHandlers()
    {
      _bootstrapperModel.BootstrapperApplication.WrapperCacheAcquireProgress -= CacheAquireProgress;
      _bootstrapperModel.BootstrapperApplication.WrapperExecuteProgress -= ExecuteProgress;
    }

    private void CacheAquireProgress(object sender, CacheAcquireProgressEventArgs e)
    {
      _cacheProgress = e.OverallPercentage;
      Progress = (_cacheProgress + _executeProgress) / 2;
    }

    private void ExecuteProgress(object sender, ExecuteProgressEventArgs e)
    {
      string packageName = Enum.TryParse(e.PackageId, out PackageId packageId) ? packageId.ToString() : "...";

      _executeProgress = e.OverallPercentage;
      Progress = (_cacheProgress + _executeProgress) / 2;
      CurrentAction = $"Processing {packageName}";
    }
  }
}
