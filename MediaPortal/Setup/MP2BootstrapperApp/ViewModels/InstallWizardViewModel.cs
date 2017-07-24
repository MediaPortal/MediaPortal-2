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

    #region Fields

    private readonly BootstrapperApplicationModel _bootstrapperApplicationModel;
    private InstallWizardPageViewModelBase _currentPage;
    private ReadOnlyCollection<BundlePackage> _bundlePackages;
    private InstallState _state;

    #endregion

    public InstallWizardViewModel(BootstrapperApplicationModel model)
    {
      _bootstrapperApplicationModel = model;
      State = InstallState.Initializing;

      WireUpEventHandlers();
      ComputeBundlePackages();

      Wizard wizard = new Wizard(new InstallWelcomeStep(this));

      NextCommand = new DelegateCommand(() => wizard.GoNext(), () => wizard.CanGoNext());
      BackCommand = new DelegateCommand(() => wizard.GoBack(), () => wizard.CanGoBack());
      CancelCommand = new DelegateCommand(() => CancelInstall(), () => State != InstallState.Canceled);
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

    public string Header { get; set; }

    public ICommand CancelCommand { get; private set; }
    public ICommand NextCommand { get; private set; }
    public ICommand BackCommand { get; private set; }

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
        }

        RaisePropertyChanged();
        Refresh();
      }
    }

    public ReadOnlyCollection<BundlePackage> BundlePackages
    {
      get { return _bundlePackages; }
    }

    private void CancelInstall()
    {
      _bootstrapperApplicationModel.LogMessage("Cancelling...");
      if (State == InstallState.Applaying)
      {
        State = InstallState.Canceled;
      }
      else
      {
        MP2BootstrapperApplication.Dispatcher.InvokeShutdown();
      }
    }

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
      _bootstrapperApplicationModel.FinalResult = e.Status;
      MP2BootstrapperApplication.Dispatcher.InvokeShutdown();
    }

    protected void PlanPackageBegin(object sender, PlanPackageBeginEventArgs planPackageBeginEventArgs)
    {
      string packageId = planPackageBeginEventArgs.PackageId;
      BundlePackage package = BundlePackages.FirstOrDefault(p => p.Id == packageId);
      planPackageBeginEventArgs.State = package.RequestedInstallState;
    }

    private void Refresh()
    {
      MP2BootstrapperApplication.Dispatcher.Invoke(() =>
      {
        ((DelegateCommand) NextCommand).RaiseCanExecuteChanged();
        ((DelegateCommand) BackCommand).RaiseCanExecuteChanged();
        ((DelegateCommand) CancelCommand).RaiseCanExecuteChanged();
      });
    }

    private void WireUpEventHandlers()
    {
      _bootstrapperApplicationModel.BootstrapperApplication.DetectPackageComplete += DetectedPackageComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.PlanComplete += PlanComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.ApplyComplete += ApplyComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.ApplyBegin += ApplyBegin;
      _bootstrapperApplicationModel.BootstrapperApplication.ExecutePackageBegin += ExecutePackageBegin;
      _bootstrapperApplicationModel.BootstrapperApplication.ExecutePackageComplete += ExecutePackageComplete;
      _bootstrapperApplicationModel.BootstrapperApplication.PlanPackageBegin += PlanPackageBegin;

      _bootstrapperApplicationModel.BootstrapperApplication.ResolveSource += (sender, args) =>
      {
        if (!string.IsNullOrEmpty(args.DownloadSource))
        {
          args.Result = Result.Download;
          _bootstrapperApplicationModel.LogMessage("Called download");
        }
        else
        {
          args.Result = Result.Ok;
        }
      };
    }

    private void ComputeBundlePackages()
    {
      IEnumerable<BundlePackage> packages = new List<BundlePackage>();

      XNamespace manifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";

      string manifestPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (manifestPath != null)
      {
        string bootstrapperDataFilePath = Path.Combine(manifestPath, "BootstrapperApplicationData.xml");
        XElement bundleManifestData;

        using (var reader = new StreamReader(bootstrapperDataFilePath))
        {
          var xml = reader.ReadToEnd();
          var xDoc = XDocument.Parse(xml);
          bundleManifestData = xDoc.Element(manifestNamespace + "BootstrapperApplicationData");
        }

        var mbaPrereqs = bundleManifestData?.Descendants(manifestNamespace + "WixMbaPrereqInformation")
          .Select(x => new BootstrapperAppPrereqPackage(x))
          .ToList();

        packages = bundleManifestData?.Descendants(manifestNamespace + "WixPackageProperties")
          .Select(x => new BundlePackage(x))
          .Where(pkg => !mbaPrereqs.Any(preReq => preReq.PackageId == pkg.Id));
      }

      _bundlePackages = new ReadOnlyCollection<BundlePackage>(packages.ToList());
    }
  }
}
