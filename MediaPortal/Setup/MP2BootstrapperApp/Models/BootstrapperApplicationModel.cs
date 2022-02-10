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
    private readonly PackageContext _packageContext;

    public BootstrapperApplicationModel(IBootstrapperApp bootstreApplication)
    {
      BootstrapperApplication = bootstreApplication;
      _hwnd = IntPtr.Zero;
      _packageContext = new PackageContext();
      ComputeBundlePackages();
      WireUpEventHandlers();
    }

    public IBootstrapperApp BootstrapperApplication { get; }
   
    public int FinalResult { get; set; }

    public ReadOnlyCollection<BundlePackage> BundlePackages { get; private set; }

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
      DetectionState = e.Operation == RelatedOperation.Downgrade ? DetectionState.Newer : DetectionState.Older;
    }

    protected void DetectedPackageComplete(object sender, DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      UpdatePackageCurrentState(detectPackageCompleteEventArgs);
    }

    private void UpdatePackageCurrentState(DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      if (Enum.TryParse(detectPackageCompleteEventArgs.PackageId, out PackageId detectedPackageId))
      {
        BundlePackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.GetId() == detectedPackageId);
        if (bundlePackage != null)
        {
          PackageId bundlePackageId = bundlePackage.GetId();
          Version installed = _packageContext.GetInstalledVersion(bundlePackageId);
          bundlePackage.InstalledVersion = installed;
          bundlePackage.CurrentInstallState = detectPackageCompleteEventArgs.State;
        }
      }
    }

    private void DetectpackageFeature(object sender, DetectMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        BundlePackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.GetId() == detectedPackageId);
        if (bundlePackage != null)
        {
          bundlePackage.FeatureStates[e.FeatureId] = e.State;
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
        BundlePackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.GetId() == detectedPackageId);
        if (bundlePackage != null && bundlePackage.FeatureStates.TryGetValue(e.FeatureId, out FeatureState featureState))
        {
          e.State = featureState;
        }
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
      // Don't override the BA's default state when uninstalling or repairing, let it use
      // the appropriate state based on the packages' current install state.
      if (PlannedAction == LaunchAction.Uninstall || PlannedAction == LaunchAction.Repair)
        return;

      if (Enum.TryParse(planPackageBeginEventArgs.PackageId, out PackageId id))
      {
        BundlePackage bundlePackage = BundlePackages.FirstOrDefault(p => p.GetId() == id);
        if (bundlePackage != null)
        {
          // All packages should have the correct requested state by default for a complete installation (determined based on InstallCondition),
          // any that aren't already requested Present shouldn't be changed as we only set Requested packages to Absent (rather than vice versa)
          // when doing a partial install of only the client/server. Any packages marked absent before this point are either already present or
          // not valid for this machine (e.g. a 64bit package on a 32bit machine).
          if (planPackageBeginEventArgs.State == RequestState.Present)
            planPackageBeginEventArgs.State = bundlePackage.RequestedInstallState;
        }
      }
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
      BootstrapperApplication.DetectPackageComplete += DetectedPackageComplete;
      BootstrapperApplication.DetectMsiFeature += DetectpackageFeature;
      BootstrapperApplication.DetectComplete += DetectComplete;
      BootstrapperApplication.ApplyComplete += ApplyComplete;
      BootstrapperApplication.PlanPackageBegin += PlanPackageBegin;
      BootstrapperApplication.PlanMsiFeature += PlanMsiFeature;
      BootstrapperApplication.PlanComplete += PlanComplete;
      BootstrapperApplication.ResolveSource += ResolveSource;
    }

    private void ComputeBundlePackages()
    {
      IEnumerable<BundlePackage> packages = new List<BundlePackage>();

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
        IList<BootstrapperAppPrereqPackage> mbaPrereqPackages = bundleManifestData?.Descendants(manifestNamespace + wixMbaPrereqInfo)
          .Select(x => new BootstrapperAppPrereqPackage(x))
          .ToList();

        const string wixPackageProperties = "WixPackageProperties";
        packages = bundleManifestData?.Descendants(manifestNamespace + wixPackageProperties)
          .Select(x => new BundlePackage(x))
          .Where(pkg => mbaPrereqPackages.All(preReq => preReq.PackageId != pkg.GetId()));
      }

      BundlePackages = packages != null
        ? new ReadOnlyCollection<BundlePackage>(packages.ToList())
        : new ReadOnlyCollection<BundlePackage>(new List<BundlePackage>());
    }
  }
}
