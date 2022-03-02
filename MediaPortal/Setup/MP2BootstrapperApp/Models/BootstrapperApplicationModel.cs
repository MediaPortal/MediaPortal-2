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

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.ChainPackages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Linq;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// Model class for the <see cref="MP2BootstrapperApplication"/>
  /// </summary>
  public class BootstrapperApplicationModel : IBootstrapperApplicationModel
  {
    private IntPtr _hwnd;

    public BootstrapperApplicationModel(IBootstrapperApp bootstreApplication)
    {
      BootstrapperApplication = bootstreApplication;
      _hwnd = IntPtr.Zero;
      ComputeBundlePackages();
      WireUpEventHandlers();
    }

    public IBootstrapperApp BootstrapperApplication { get; }
   
    public int FinalResult { get; set; }

    public ReadOnlyCollection<IBundlePackage> BundlePackages { get; private set; }

    public DetectionState DetectionState { get; set; } = DetectionState.Absent;

    public LaunchAction PlannedAction { get; set; } = LaunchAction.Unknown;

    public InstallState InstallState { get; set; } = InstallState.Initializing;

    public bool Cancelled { get; set; }

    public void SetWindowHandle(Window view)
    {
      _hwnd = new WindowInteropHelper(view).Handle;
    }

    public void PlanAction(LaunchAction action)
    {
      InstallState = InstallState.Planning;
      PlannedAction = action;
      BootstrapperApplication.Plan(action);
    }

    public void ApplyAction()
    {
      InstallState = InstallState.Applying;
      BootstrapperApplication.Apply(_hwnd);
    }

    public void LogMessage(LogLevel logLevel, string message)
    {
      BootstrapperApplication.Log(logLevel, message);
    }

    private void DetectBegin(object sender, DetectBeginEventArgs e)
    {
      InstallState = InstallState.Detecting;
      DetectionState = e.Installed ? DetectionState.Present : DetectionState.Absent;
    }

    private void DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
      DetectionState = e.Operation == RelatedOperation.Downgrade ? DetectionState.Older : DetectionState.Newer;
    }

    protected void DetectPackageComplete(object sender, DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      UpdatePackageCurrentState(detectPackageCompleteEventArgs);
    }

    private void UpdatePackageCurrentState(DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      if (Enum.TryParse(detectPackageCompleteEventArgs.PackageId, out PackageId detectedPackageId))
      {
        IBundlePackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId);
        if (bundlePackage != null)
          bundlePackage.CurrentInstallState = detectPackageCompleteEventArgs.State;
      }
    }

    private void DetectMsiFeature(object sender, DetectMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        IBundleMsiPackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId) as IBundleMsiPackage;
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.FeatureName == e.FeatureId);
        if (bundleFeature != null)
          bundleFeature.CurrentFeatureState = e.State;
      }
    }

    private void DetectRelatedMsiPackage(object sender, DetectRelatedMsiPackageEventArgs e)
    {
      if (e.Operation == RelatedOperation.MajorUpgrade)
      {
        IBundleMsiPackage bundledPackage = BundlePackages.FirstOrDefault(p => p.Id == e.PackageId) as IBundleMsiPackage;
        if (bundledPackage != null)
        {
          ProductInstallation installedPackage = new ProductInstallation(e.ProductCode);
          bundledPackage.InstalledVersion = e.Version;
          foreach (FeatureInstallation feature in installedPackage.Features)
          {
            IBundlePackageFeature bundleFeature = bundledPackage.Features.FirstOrDefault(f => f.FeatureName == feature.FeatureName);
            if (bundleFeature != null)
              bundleFeature.PreviousVersionInstalled = feature.State == Microsoft.Deployment.WindowsInstaller.InstallState.Local;
          }
        }        
      }
    }

    private void DetectComplete(object sender, DetectCompleteEventArgs e)
    {
      InstallState = Hresult.Succeeded(e.Status) ? InstallState.Waiting : InstallState.Failed;
    }

    private void PlanMsiFeature(object sender, PlanMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        IBundleMsiPackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId) as IBundleMsiPackage;
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.FeatureName == e.FeatureId);
        if (bundleFeature != null)
          e.State = bundleFeature.RequestedFeatureState;
      }
    }

    private void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (!Cancelled && Hresult.Succeeded(e.Status))
      {
        InstallState = InstallState.Applying;
        ApplyAction();
      }
      else
      {
        InstallState = InstallState.Failed;
      }
    }

    protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
      FinalResult = e.Status;
      InstallState = Hresult.Succeeded(e.Status) ? InstallState.Applied : InstallState.Failed;
    }

    protected void PlanPackageBegin(object sender, PlanPackageBeginEventArgs planPackageBeginEventArgs)
    {
      UpdatePackageRequestState(planPackageBeginEventArgs);
    }

    private void UpdatePackageRequestState(PlanPackageBeginEventArgs planPackageBeginEventArgs)
    {
      IBundlePackage bundlePackage = BundlePackages.FirstOrDefault(p => p.Id == planPackageBeginEventArgs.PackageId);
      // Packages not present in BundlePackages are bootstrapper prerequisite packages (currently only .net),
      // We shouldn't ever need to take action on these packages, they are already installed before the bootstrapper
      // app is run and repairing .net takes a long time, often require a restart, and is unlikely to be the cause
      // of issues with the MediaPortal 2 installation.
      if (bundlePackage == null)
      {
        planPackageBeginEventArgs.State = RequestState.None;
        return;
      }

      // Don't override the BA's default state when uninstalling, let it use
      // the appropriate state based on the packages' current install state.
      if (PlannedAction == LaunchAction.Uninstall)
        return;

      planPackageBeginEventArgs.State = bundlePackage.RequestedInstallState;
    }

    private void ResolveSource(object sender, ResolveSourceEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.DownloadSource))
      {
        e.Result = Result.Download;
      }
      else
      {
        e.Result = Result.Ok;
      }
    }

    private void WireUpEventHandlers()
    {
      BootstrapperApplication.DetectBegin += DetectBegin;
      BootstrapperApplication.DetectRelatedBundle += DetectRelatedBundle;
      BootstrapperApplication.DetectPackageComplete += DetectPackageComplete;
      BootstrapperApplication.DetectMsiFeature += DetectMsiFeature;
      BootstrapperApplication.DetectRelatedMsiPackage += DetectRelatedMsiPackage;
      BootstrapperApplication.DetectComplete += DetectComplete;
      BootstrapperApplication.ApplyComplete += ApplyComplete;
      BootstrapperApplication.PlanPackageBegin += PlanPackageBegin;
      BootstrapperApplication.PlanMsiFeature += PlanMsiFeature;
      BootstrapperApplication.PlanComplete += PlanComplete;
      BootstrapperApplication.ResolveSource += ResolveSource;
    }

    private void ComputeBundlePackages()
    {
      IList<IBundlePackage> packages = new List<IBundlePackage>();

      XNamespace manifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";

      string manifestPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (manifestPath != null)
      {
        const string bootstrapperApplicationData = "BootstrapperApplicationData";
        const string xmlExtension = ".xml";
        string bootstrapperDataFilePath = Path.Combine(manifestPath, bootstrapperApplicationData + xmlExtension);
        XElement bundleManifestData;

        using (StreamReader reader = new StreamReader(bootstrapperDataFilePath))
        {
          string xml = reader.ReadToEnd();
          XDocument xDoc = XDocument.Parse(xml);
          bundleManifestData = xDoc.Element(manifestNamespace + bootstrapperApplicationData);
        }

        const string wixMbaPrereqInfo = "WixMbaPrereqInformation";
        IList<string> mbaPrereqPackages = bundleManifestData?.Descendants(manifestNamespace + wixMbaPrereqInfo)
          .Select(x => x.Attribute("PackageId")?.Value)
          .ToList();

        BundlePackageFactory bundlePackageFactory = new BundlePackageFactory(new PackageContext());

        const string wixPackageProperties = "WixPackageProperties";
        packages = bundleManifestData?.Descendants(manifestNamespace + wixPackageProperties)
          .Where(x => mbaPrereqPackages.All(preReq => preReq != x.Attribute("Package")?.Value))
          .Select(x => bundlePackageFactory.CreatePackage(x)).ToList();

        const string wixPackageFeatureInfo = "WixPackageFeatureInfo";
        IEnumerable<IBundlePackageFeature> features = bundleManifestData?.Descendants(manifestNamespace + wixPackageFeatureInfo)
          .Select(x => bundlePackageFactory.CreatePackageFeature(x));
        foreach (IBundlePackageFeature feature in features)
        {
          IBundleMsiPackage parent = packages.FirstOrDefault(p => p.Id == feature.Package) as IBundleMsiPackage;
          if (parent != null)
            parent.Features.Add(feature);
        }
      }

      BundlePackages = packages != null
        ? new ReadOnlyCollection<IBundlePackage>(packages)
        : new ReadOnlyCollection<IBundlePackage>(new List<IBundlePackage>());
    }
  }
}
