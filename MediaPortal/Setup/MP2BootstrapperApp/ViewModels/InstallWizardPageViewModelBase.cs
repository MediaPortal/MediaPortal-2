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
using MP2BootstrapperApp.Models;
using MP2BootstrapperApp.WizardSteps;
using Prism.Mvvm;
using System;

namespace MP2BootstrapperApp.ViewModels
{
  public abstract class InstallWizardPageViewModelBase : BindableBase
  {
    protected IStep _step;

    private string _header;
    private string _subHeader;
    private string _buttonNextContent = "Next";
    private string _buttonBackContent = "Back";
    private string _buttonCancelContent = "Abort";

    public InstallWizardPageViewModelBase(IStep step)
    {
      _step = step;
    }

    public string Header
    {
      get { return _header; }
      set { SetProperty(ref _header, value); }
    }

    public string SubHeader
    {
      get { return _subHeader; }
      set { SetProperty(ref _subHeader, value); }
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

    public event EventHandler ButtonStateChanged;

    protected virtual void RaiseButtonStateChanged()
    {
      ButtonStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Attach()
    {
    }

    public virtual void Detach()
    {

    }

    protected Package CreatePackage(IBundlePackage bundlePackage)
    {
      return new Package
      {
        BundleVersion = bundlePackage.Version,
        InstalledVersion = bundlePackage.InstalledVersion,
        ImagePath = @"..\resources\" + bundlePackage.PackageId + ".png",
        Id = bundlePackage.Id,
        DisplayName = bundlePackage.DisplayName,
        Description = bundlePackage.Description,
        PackageState = bundlePackage.CurrentInstallState,
        RequestState = bundlePackage.RequestedInstallState
      };
    }

    protected Package CreatePackageFeature(IBundlePackage bundlePackage, IBundlePackageFeature feature)
    {
      return new Package
      {
        BundleVersion = bundlePackage.Version,
        InstalledVersion = (feature.PreviousVersionInstalled || feature.CurrentFeatureState == FeatureState.Local) ? bundlePackage.InstalledVersion : new Version(),
        ImagePath = @"..\resources\" + feature.FeatureName + ".png",
        Id = feature.FeatureName,
        DisplayName = feature.Title,
        Description = feature.Description,
        PackageState = bundlePackage.CurrentInstallState,
        RequestState = bundlePackage.RequestedInstallState
      };
    }
  }
}
