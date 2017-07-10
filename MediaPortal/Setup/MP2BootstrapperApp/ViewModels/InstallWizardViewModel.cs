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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Xml.Linq;
using MP2BootstrapperApp.Models;
using Prism.Mvvm;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallWizardViewModel : BindableBase
  {
    #region Fields

    private readonly BootstrapperApplicationModel _bootstrapperApplicationModel;
    private InstallWizardPageViewModelBase _currentPage;
    private ReadOnlyCollection<InstallWizardPageViewModelBase> _pages;
    private ReadOnlyCollection<BundlePackage> _bundlePackages;

    #endregion

    public InstallWizardViewModel(BootstrapperApplicationModel model)
    {
      _bootstrapperApplicationModel = model;
      CurrentPage = Pages[0];
      ComputeBundlePackages();
    }

    public ICommand CancelCommand { get; private set; }

    public InstallWizardPageViewModelBase CurrentPage
    {
      get { return _currentPage; }
      private set
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
      }
    }

    public ReadOnlyCollection<InstallWizardPageViewModelBase> Pages
    {
      get
      {
        if (_pages == null)
        {
          CreatePages();
        }
        return _pages;
      }
    }

    public ReadOnlyCollection<BundlePackage> BundlePackages
    {
      get { return _bundlePackages; }
    }

    private void CreatePages()
    {
      var existInstallVm = new InstallExistTypePageViewModel(_bootstrapperApplicationModel);
      var newInstallVm = new InstallNewTypePageViewModel(_bootstrapperApplicationModel);
      var overviewVm = new InstallOverviewPageViewModel(_bootstrapperApplicationModel);
      var finishVm = new InstallFinishPageViewModel(_bootstrapperApplicationModel);

      var pages = new List<InstallWizardPageViewModelBase>
      {
        new InstallWelcomePageViewModel(),
        existInstallVm,
        newInstallVm,
        overviewVm,
        finishVm
      };

      _pages = new ReadOnlyCollection<InstallWizardPageViewModelBase>(pages);
    }

    private void ComputeBundlePackages()
    {
      IList<BundlePackage> packages = null;

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

        packages = (IList<BundlePackage>)bundleManifestData?.Descendants(manifestNamespace + "WixPackageProperties")
          .Select(x => new BundlePackage(x))
          .Where(pkg => !mbaPrereqs.Any(preReq => preReq.PackageId == pkg.Id));
      }

      _bundlePackages = new ReadOnlyCollection<BundlePackage>(packages);
    }
  }
}
